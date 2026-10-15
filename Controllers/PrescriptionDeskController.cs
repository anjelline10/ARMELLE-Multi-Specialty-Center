using Armelle.Data;
using Armelle.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Armelle.Controllers;
public class PrescriptionDeskController(ClinicDb db, IWebHostEnvironment env) : Controller
{
    private bool IsOwner => HttpContext.Session.GetString("Role") == "Owner";
    public IActionResult Index()
    {
        if (!IsOwner) return RedirectToAction("OwnerLogin", "Auth");
        ViewBag.Patients = db.Patients.OrderBy(x => x.Name).ToList();
        ViewBag.Releases = db.PrescriptionReleases.ToList();
        return View(db.Consultations.OrderByDescending(x => x.Date).Take(250).ToList());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(string patient, string doctor, string diagnosis, string prescription, string notes)
    {
        if (!IsOwner) return Forbid();
        if (string.IsNullOrWhiteSpace(patient) || string.IsNullOrWhiteSpace(doctor) || string.IsNullOrWhiteSpace(prescription))
        { TempData["PrescriptionError"] = "Patient, prescribing physician and prescription details are required."; return RedirectToAction(nameof(Index)); }
        if (!db.Patients.Any(p => p.Name == patient))
        { TempData["PrescriptionError"] = "Select a patient from the clinic database."; return RedirectToAction(nameof(Index)); }
        db.Consultations.Add(new Consultation { Patient = patient, Doctor = doctor.Trim(), Diagnosis = (diagnosis ?? "").Trim(), Prescription = prescription.Trim(), Notes = (notes ?? "").Trim(), Date = DateTime.Now });
        db.SaveChanges();
        TempData["PrescriptionMessage"] = "Draft recorded. Obtain and record actual physician authorization before releasing it to the patient.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6_000_000)]
    public IActionResult UploadApproved(int consultationId, string physicianName, string licenseNumber, bool confirmed, IFormFile? document)
    {
        if (!IsOwner) return Forbid();
        var consult = db.Consultations.Find(consultationId);
        if (consult == null || string.IsNullOrWhiteSpace(consult.Prescription)) return NotFound();
        if (db.PrescriptionReleases.Any(r => r.ConsultationId == consultationId))
        { TempData["PrescriptionError"] = "Already released. Create a new consultation for a revision."; return RedirectToAction(nameof(Index)); }
        if (!confirmed || string.IsNullOrWhiteSpace(physicianName) || string.IsNullOrWhiteSpace(licenseNumber))
        { TempData["PrescriptionError"] = "Physician approval, name and license number are required."; return RedirectToAction(nameof(Index)); }
        string savedName = "";
        if (document != null && document.Length > 0)
        {
            var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (document.Length > 5_000_000 || !new[] { ".pdf", ".png", ".jpg", ".jpeg", ".webp" }.Contains(ext))
            { TempData["PrescriptionError"] = "Only PDF or image files up to 5 MB are allowed."; return RedirectToAction(nameof(Index)); }
            // Store outside wwwroot. Files can only be downloaded through an authorized controller action.
            var dir = Path.Combine(env.ContentRootPath, "App_Data", "private_prescriptions");
            Directory.CreateDirectory(dir);
            savedName = Guid.NewGuid().ToString("N") + ext;
            using var stream = System.IO.File.Create(Path.Combine(dir, savedName));
            document.CopyTo(stream);
        }
        db.PrescriptionReleases.Add(new PrescriptionRelease { ConsultationId = consultationId, PhysicianName = physicianName.Trim(), LicenseNumber = licenseNumber.Trim(), RecordedBy = HttpContext.Session.GetString("User") ?? "Owner", ReleasedAt = DateTime.UtcNow, PriceBreakdown = "", EstimatedMedicineTotal = 0 });
        if (savedName != "") db.PrescriptionDocuments.Add(new PrescriptionDocument { ConsultationId = consultationId, StoredFileName = savedName, OriginalFileName = Path.GetFileName(document!.FileName), UploadedAtUtc = DateTime.UtcNow });
        db.SaveChanges();
        TempData["PrescriptionMessage"] = "Released to this patient's private portal. No external email or SMS sent.";
        return RedirectToAction(nameof(Index));
    }
    public IActionResult Download(int id)
    {
        var role = HttpContext.Session.GetString("Role");
        var user = HttpContext.Session.GetString("User");
        if (user == null || (role != "Owner" && role != "Patient")) return Unauthorized();
        var doc = db.PrescriptionDocuments.FirstOrDefault(x => x.Id == id);
        if (doc == null) return NotFound();
        var consult = db.Consultations.Find(doc.ConsultationId);
        if (consult == null || (role == "Patient" && consult.Patient != user)) return NotFound();
        if (!db.PrescriptionReleases.Any(x => x.ConsultationId == consult.Id)) return NotFound();
        var path = Path.Combine(env.ContentRootPath, "App_Data", "private_prescriptions", doc.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch { ".pdf" => "application/pdf", ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
        return PhysicalFile(path, contentType, doc.OriginalFileName);
    }
}
