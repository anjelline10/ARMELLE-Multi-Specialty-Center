
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Armelle.Data;
using Armelle.Models;
using Armelle.Services;

namespace Armelle.Controllers
{
    public class AuthController : Controller
    {
        private readonly ClinicDb _db;

        public AuthController(ClinicDb db)
        {
            _db = db;
        }

        // PATIENT LOGIN PAGE
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // PATIENT LOGIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(
            string username,
            string password,
            string? returnUrl = null)
        {
            var user = _db.AppUsers.FirstOrDefault(
                x => x.Username == username &&
                     x.Role == "Patient");

            if (user == null ||
                !PasswordSecurity.VerifyAndUpgrade(
                    user, password, _db))
            {
                ViewBag.Error =
                    "Invalid patient username or password.";

                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            SetSession(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Patient");
        }

        // OWNER LOGIN PAGE
        [HttpGet]
        public IActionResult OwnerLogin()
        {
            return View();
        }

        // OWNER LOGIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OwnerLogin(
            string username,
            string password)
        {
            username = username?.Trim() ?? "";
            var user = _db.AppUsers.FirstOrDefault(
                x => x.Username == username &&
                     x.Role == "Owner");

            if (user == null ||
                !PasswordSecurity.VerifyAndUpgrade(
                    user, password, _db))
            {
                ViewBag.Error =
                    "Invalid owner username or password.";

                return View();
            }

            SetSession(user);

            return RedirectToAction("Index", "Home");
        }

        // OLD WORKFORCE ROUTES
        // Redirect to Owner Login.
        [HttpGet]
        public IActionResult WorkforceLogin()
        {
            return RedirectToAction(nameof(OwnerLogin));
        }

        [HttpGet]
        public IActionResult WorkforceGate()
        {
            return RedirectToAction(nameof(OwnerLogin));
        }

        [HttpGet]
        public IActionResult WorkforceMemberLogin()
        {
            return RedirectToAction(nameof(OwnerLogin));
        }

        [HttpGet]
        public IActionResult WorkforceDashboard()
        {
            return RedirectToAction(nameof(OwnerLogin));
        }

        // PATIENT REGISTRATION PAGE
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // PATIENT REGISTRATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(
            string username,
            string password,
            string? returnUrl = null)
        {
            username = username?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                password.Length < 10)
            {
                ViewBag.Error =
                    "Enter a username and password of at least 10 characters.";

                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (_db.AppUsers.Any(x => x.Username == username))
            {
                ViewBag.Error = "Username already exists.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var user = new AppUser
            {
                Username = username,
                Password = PasswordSecurity.Hash(password),
                Role = "Patient"
            };

            _db.AppUsers.Add(user);
            _db.SaveChanges();

            SetSession(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Patient");
        }

        // SESSION MANAGEMENT
        private void SetSession(AppUser user)
        {
            HttpContext.Session.Clear();

            HttpContext.Session.SetString(
                "User", user.Username);

            HttpContext.Session.SetString(
                "Role", user.Role);
        }

        // LOGOUT
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Public");
        }
    }
}
