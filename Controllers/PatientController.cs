using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Armelle.Data;
using Armelle.Models;
using Armelle.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Armelle.Controllers;

public class PatientController(ClinicDb db, IWebHostEnvironment env) : Controller
{
    private AppUser? CurrentPatient()
    {
        if (HttpContext.Session.GetString("Role") != "Patient") return null;
        var username = HttpContext.Session.GetString("User");
        return string.IsNullOrEmpty(username) ? null : db.AppUsers.FirstOrDefault(x => x.Username == username && x.Role == "Patient");
    }

    public IActionResult Index()
    {
        var account = CurrentPatient();
        if (account == null) return RedirectToAction("Login", "Auth");
        var u = account.Username;
        ViewBag.User = u;
        ViewBag.Profile = db.PatientProfiles.AsNoTracking().FirstOrDefault(x => x.AppUserId == account.Id);
        ViewBag.PromoRedemptions = db.PromotionRedemptions.Where(x => x.PatientUserId == account.Id).OrderByDescending(x => x.RedeemedAtUtc).ToList();
        ViewBag.ActivePromotions = db.ClinicPromotions.Where(x => x.IsPublished && x.StartsAt <= DateTime.Today && x.EndsAt >= DateTime.Today).OrderBy(x => x.EndsAt).ToList();
        ViewBag.TotalPromos = db.PromotionRedemptions.Count(x => x.PatientUserId == account.Id);
        // Dashboard counters always use all persisted records for the signed-in patient.
        // Older bookings may have been stored under the patient's profile full name.
        var profileName = db.PatientProfiles.AsNoTracking()
            .Where(x => x.AppUserId == account.Id).Select(x => x.FullName).FirstOrDefault();
        var hasProfileName = !string.IsNullOrWhiteSpace(profileName) && profileName != u;
        ViewBag.TotalAppointments = db.Appointments.Count(x => x.Patient == u || (hasProfileName && x.Patient == profileName));
        ViewBag.TotalLabs = db.LabRequests.Count(x => x.Patient == u || (hasProfileName && x.Patient == profileName));
        ViewBag.TotalBills = db.Invoices.Count(x => x.Patient == u || (hasProfileName && x.Patient == profileName));

        // Patient-specific history; never expose other patients' records.
        ViewBag.HistoryAppointments = db.Appointments.AsNoTracking().Where(x => x.Patient == u || (hasProfileName && x.Patient == profileName)).OrderByDescending(x => x.Schedule).Take(7).ToList();
        ViewBag.HistoryConsultations = db.Consultations.AsNoTracking().Where(x => x.Patient == u || (hasProfileName && x.Patient == profileName)).OrderByDescending(x => x.Date).Take(7).ToList();
        ViewBag.HistoryPrescriptions = db.Consultations.AsNoTracking().Where(x => (x.Patient == u || (hasProfileName && x.Patient == profileName)) && x.Prescription != null && x.Prescription != "").OrderByDescending(x => x.Date).Take(7).ToList();
        ViewBag.HistoryLabs = db.LabRequests.AsNoTracking().Where(x => x.Patient == u || (hasProfileName && x.Patient == profileName)).OrderByDescending(x => x.Requested).Take(7).ToList();
        ViewBag.HistoryTransactions = db.Invoices.AsNoTracking().Where(x => x.Patient == u || (hasProfileName && x.Patient == profileName)).OrderByDescending(x => x.Date).Take(7).ToList();
        ViewBag.Upcoming = db.Appointments.Where(x => (x.Patient == u || (hasProfileName && x.Patient == profileName)) && x.Schedule >= DateTime.Today && x.Status != "Cancelled").OrderBy(x => x.Schedule).Take(5).ToList();
        ViewBag.Labs = db.LabRequests.Where(x => x.Patient == u || (hasProfileName && x.Patient == profileName)).OrderByDescending(x => x.Requested).Take(5).ToList();
        ViewBag.Bills = db.Invoices.Where(x => x.Patient == u || (hasProfileName && x.Patient == profileName)).OrderByDescending(x => x.Date).Take(5).ToList();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> SaveProfile(
        string FullName, DateTime? BirthDate, string? Sex, string? Contact,
        string? Email, string? Address, string? EmergencyContactName,
        string? EmergencyRelationship, string? EmergencyContactNumber,
        string? Hmo, string? HmoMemberNumber, IFormFile? ProfilePhoto,
        bool RemovePhoto = false)
    {
        var account = CurrentPatient();
        if (account == null) return Unauthorized(new { message = "Please log in again." });
        FullName = (FullName ?? "").Trim();
        if (FullName.Length is < 1 or > 120) return BadRequest(new { message = "Full name is required (maximum 120 characters)." });
        if (BirthDate.HasValue && (BirthDate.Value.Date > DateTime.Today || BirthDate.Value.Year < 1900))
            return BadRequest(new { message = "Invalid birth date." });
        if (!string.IsNullOrWhiteSpace(Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(Email.Trim()))
            return BadRequest(new { message = "Invalid email address." });
        if (ProfilePhoto != null && ProfilePhoto.Length > 0)
        {
            if (ProfilePhoto.Length > 2 * 1024 * 1024) return BadRequest(new { message = "Photo must be 2 MB or smaller." });
            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(ProfilePhoto.ContentType.ToLowerInvariant())) return BadRequest(new { message = "Only JPG, PNG and WebP photos are supported." });
            await using var source = ProfilePhoto.OpenReadStream();
            var header = new byte[12];
            var read = await source.ReadAsync(header.AsMemory(0, header.Length));
            var jpg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var png = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137,80,78,71,13,10,26,10 });
            var webp = read >= 12 && System.Text.Encoding.ASCII.GetString(header,0,4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header,8,4) == "WEBP";
            if (!((jpg && ProfilePhoto.ContentType == "image/jpeg") || (png && ProfilePhoto.ContentType == "image/png") || (webp && ProfilePhoto.ContentType == "image/webp")))
                return BadRequest(new { message = "Photo content does not match the selected format." });
        }
        var profile = await db.PatientProfiles.FirstOrDefaultAsync(x => x.AppUserId == account.Id);
        if (profile == null) { profile = new PatientProfile { AppUserId = account.Id }; db.PatientProfiles.Add(profile); }
        profile.FullName = FullName;
        profile.BirthDate = BirthDate;
        profile.Sex = Limit(Sex, 40);
        profile.Contact = Limit(Contact, 20);
        profile.Email = Limit(Email, 160);
        profile.Address = Limit(Address, 350);
        profile.EmergencyContactName = Limit(EmergencyContactName, 120);
        profile.EmergencyRelationship = Limit(EmergencyRelationship, 70);
        profile.EmergencyContactNumber = Limit(EmergencyContactNumber, 20);
        profile.Hmo = Limit(Hmo, 120);
        profile.HmoMemberNumber = Limit(HmoMemberNumber, 80);
        if (RemovePhoto) profile.PhotoPath = "";
        if (ProfilePhoto != null && ProfilePhoto.Length > 0)
        {
            var extension = ProfilePhoto.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
            var directory = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "patient-profiles");
            Directory.CreateDirectory(directory);
            var fileName = $"patient-{account.Id}-{Guid.NewGuid():N}{extension}";
            await using var output = System.IO.File.Create(Path.Combine(directory, fileName));
            await ProfilePhoto.CopyToAsync(output);
            profile.PhotoPath = "/uploads/patient-profiles/" + fileName;
        }
        await db.SaveChangesAsync();
        return Json(new { saved = true, fullName = profile.FullName, photoPath = profile.PhotoPath });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
    {
        var account = CurrentPatient();
        if (account == null) return Unauthorized(new { message = "Please log in again." });
        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 8 || NewPassword.Length > 128)
            return BadRequest(new { message = "New password must be 8–128 characters." });
        if (NewPassword != ConfirmPassword) return BadRequest(new { message = "Passwords do not match." });
        if (!PasswordSecurity.VerifyAndUpgrade(account, CurrentPassword, db))
            return BadRequest(new { message = "Current password is incorrect." });
        account.Password = PasswordSecurity.Hash(NewPassword);
        await db.SaveChangesAsync();
        return Json(new { saved = true });
    }

    private static string Limit(string? value, int max) => (value ?? "").Trim()[..Math.Min((value ?? "").Trim().Length, max)];
}
