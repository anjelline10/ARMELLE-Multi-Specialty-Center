
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Armelle.Data;
using Armelle.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Armelle.Controllers
{
    public class HomeController : Controller
    {
        private readonly ClinicDb db;
        private readonly IWebHostEnvironment env;

        public HomeController(
            ClinicDb database,
            IWebHostEnvironment environment)
        {
            db = database;
            env = environment;
        }

        private bool Owner =>
            HttpContext.Session.GetString("Role") == "Owner";

        private IActionResult SignIn() =>
            RedirectToAction("OwnerLogin", "Auth");

        public class OwnerProfileDisplay
        {
            public string Key { get; set; } = "";
            public string Name { get; set; } = "";
            public string Title { get; set; } = "";
            public string PhotoPath { get; set; } = "";
        }

        private static readonly (
            string Username,
            string Name,
            string Position)[] Owners =
        {
            (
                "angeline",
                "Angeline P. Labrado",
                "CEO, COO & Owner"
            ),
            (
                "ricky",
                "Ricky Jr. B. Diestro",
                "CTO & Co-Founder"
            ),
            (
                "mica",
                "Mica P. Gonzales",
                "CFO & Co-Founder"
            )
        };

        private AppUser? CurrentOwner()
        {
            if (!Owner)
                return null;

            var username =
                HttpContext.Session.GetString("User");

            if (string.IsNullOrWhiteSpace(username))
                return null;

            return db.AppUsers.FirstOrDefault(x =>
                x.Username == username &&
                x.Role == "Owner");
        }

        private List<OwnerProfileDisplay> LoadOwners()
        {
            var result = new List<OwnerProfileDisplay>();

            foreach (var owner in Owners)
            {
                var account = db.AppUsers
                    .FirstOrDefault(x =>
                        x.Username == owner.Username &&
                        x.Role == "Owner");

                Armelle.Models.OwnerProfile? profile = null;

                if (account != null)
                {
                    profile = db.OwnerProfiles
                        .FirstOrDefault(x =>
                            x.AppUserId == account.Id);
                }

                result.Add(new OwnerProfileDisplay
                {
                    Key = owner.Username,
                    Name = profile?.DisplayName ?? owner.Name,
                    Title = profile?.Position ?? owner.Position,
                    PhotoPath = profile?.PhotoPath ?? ""
                });
            }

            return result;
        }

        public IActionResult Index()
        {
            if (!Owner)
                return SignIn();

            ViewBag.User =
                HttpContext.Session.GetString("User")
                ?? "Owner";

            ViewBag.Patients = db.Patients.Count();

            ViewBag.BranchPatients = db.Patients
                .GroupBy(x => x.Branch)
                .Select(g => new
                {
                    Branch = g.Key,
                    Count = g.Count()
                })
                .ToDictionary(
                    x => x.Branch,
                    x => x.Count);

            ViewBag.BranchAppointments = db.Appointments
                .GroupBy(x => x.Branch)
                .Select(g => new
                {
                    Branch = g.Key,
                    Count = g.Count()
                })
                .ToDictionary(
                    x => x.Branch,
                    x => x.Count);

            ViewBag.Today = db.Appointments
                .Count(x =>
                    x.Schedule.Date == DateTime.Today);

            ViewBag.Queue = db.QueueEntries
                .Count(x =>
                    x.Status != "Completed");

            ViewBag.Staff = db.Employees
                .Count(x =>
                    x.TimeIn != null &&
                    x.TimeOut == null);

            ViewBag.Revenue = db.Invoices
                .Where(x => x.Status == "Paid")
                .Sum(x =>
                    (decimal?)(x.Amount - x.Discount))
                ?? 0m;

            ViewBag.Expenses = db.Expenses
                .Sum(x => (decimal?)x.Amount)
                ?? 0m;

            ViewBag.LowStock = db.Medicines
                .Count(x => x.Stock <= 20);

            ViewBag.Doctors = db.Doctors
                .Count(x => x.IsActive);

            ViewBag.Reservations = db.Appointments
                .Count(x => x.Status == "Reserved");

            ViewBag.Employees = db.Employees.Count();
            ViewBag.Medicines = db.Medicines.Count();
            ViewBag.Appointments = db.Appointments.Count();
            ViewBag.Labs = db.LabRequests.Count();
            ViewBag.Invoices = db.Invoices.Count();

            return View();
        }

        public IActionResult ExecutiveTeam()
        {
            if (!Owner)
                return SignIn();

            ViewBag.Owners = LoadOwners();

            return View();
        }

        public IActionResult PublicPreview()
        {
            if (!Owner)
                return SignIn();

            return View();
        }

        public IActionResult OwnerSettings()
        {
            if (!Owner)
                return SignIn();

            var account = CurrentOwner();

            if (account == null)
                return SignIn();

            var ownerDefinition = Owners
                .FirstOrDefault(x =>
                    x.Username.Equals(
                        account.Username,
                        StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(
                ownerDefinition.Username))
            {
                TempData["Error"] =
                    "Owner account is not linked to a profile.";

                return RedirectToAction(nameof(Index));
            }

            var profile = db.OwnerProfiles
                .FirstOrDefault(x =>
                    x.AppUserId == account.Id);

            var allOwners = LoadOwners();

            ViewBag.OwnerName =
                profile?.DisplayName
                ?? ownerDefinition.Name;

            ViewBag.OwnerTitle =
                profile?.Position
                ?? ownerDefinition.Position;

            ViewBag.OwnerPhoto =
                profile?.PhotoPath ?? "";

            ViewBag.OwnerKey =
                account.Username;

            ViewBag.OtherOwners = allOwners
                .Where(x =>
                    !x.Key.Equals(
                        account.Username,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4_000_000)]
        public async Task<IActionResult> SaveOwnerProfile(
            string displayName,
            string ownerTitle,
            IFormFile? photo)
        {
            if (!Owner)
                return Forbid();

            var account = CurrentOwner();

            if (account == null)
                return Forbid();

            var ownerDefinition = Owners
                .FirstOrDefault(x =>
                    x.Username.Equals(
                        account.Username,
                        StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(
                ownerDefinition.Username))
                return Forbid();

            if (string.IsNullOrWhiteSpace(displayName) ||
                displayName.Trim().Length > 120)
            {
                TempData["Error"] =
                    "Enter a valid owner name.";

                return RedirectToAction(nameof(OwnerSettings));
            }

            if (string.IsNullOrWhiteSpace(ownerTitle) ||
                ownerTitle.Trim().Length > 160)
            {
                TempData["Error"] =
                    "Enter a valid position.";

                return RedirectToAction(nameof(OwnerSettings));
            }

            var profile = await db.OwnerProfiles
                .FirstOrDefaultAsync(x =>
                    x.AppUserId == account.Id);

            if (profile == null)
            {
                profile = new Armelle.Models.OwnerProfile
                {
                    AppUserId = account.Id,
                    DisplayName = ownerDefinition.Name,
                    Position = ownerDefinition.Position,
                    PhotoPath = ""
                };

                db.OwnerProfiles.Add(profile);
            }

            if (photo != null && photo.Length > 0)
            {
                var extension = Path.GetExtension(
                    photo.FileName).ToLowerInvariant();

                string[] allowed =
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (!allowed.Contains(extension) ||
                    photo.Length > 3_000_000)
                {
                    TempData["Error"] =
                        "Use JPG, PNG or WebP up to 3 MB.";

                    return RedirectToAction(nameof(OwnerSettings));
                }

                var uploadFolder = Path.Combine(
                    env.WebRootPath ??
                    Path.Combine(
                        env.ContentRootPath,
                        "wwwroot"),
                    "uploads",
                    "owners");

                Directory.CreateDirectory(uploadFolder);

                var fileName =
                    Guid.NewGuid().ToString("N") + extension;

                var physicalPath =
                    Path.Combine(uploadFolder, fileName);

                await using (var stream =
                    System.IO.File.Create(physicalPath))
                {
                    await photo.CopyToAsync(stream);
                }

                profile.PhotoPath =
                    "/uploads/owners/" + fileName;
            }

            profile.DisplayName = displayName.Trim();
            profile.Position = ownerTitle.Trim();

            await db.SaveChangesAsync();

            HttpContext.Session.SetString(
                "OwnerDisplayName",
                profile.DisplayName);

            HttpContext.Session.SetString(
                "OwnerTitle",
                profile.Position);

            HttpContext.Session.SetString(
                "OwnerPhoto",
                profile.PhotoPath);

            TempData["Success"] =
                "Owner profile updated successfully.";

            return RedirectToAction(nameof(OwnerSettings));
        }

        public IActionResult DownloadRevenueCsv()
        {
            if (!Owner)
                return Forbid();

            var amount = db.Invoices
                .Where(x => x.Status == "Paid")
                .Sum(x =>
                    (decimal?)(x.Amount - x.Discount))
                ?? 0m;

            var csv =
                "Report,Amount (PHP)\r\n" +
                "Paid revenue (all branches)," +
                amount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) +
                "\r\n";

            return File(
                new UTF8Encoding(true).GetBytes(csv),
                "text/csv",
                "ARMELLE_Revenue_" +
                DateTime.Today.ToString("yyyyMMdd") +
                ".csv");
        }
    }
}
