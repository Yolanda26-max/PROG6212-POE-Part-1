using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        // Session keys, reused by every other controller and view
        public const string RoleKey = "Role";
        public const string NameKey = "FullName";
        public const string UserIdKey = "UserId";

        private readonly ApiClient _api;

        public AccountController(ApiClient api)
        {
            _api = api;
        }

        // ---------- LOGIN ----------

        [HttpGet]
        public IActionResult Login()
        {
            // Already logged in? Skip the login page
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(ApiClient.TokenKey)))
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.PostAsync<AuthResponseDto>("api/auth/login", new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password
            });

            if (!result.Success || result.Data == null || string.IsNullOrEmpty(result.Data.Token))
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Login failed. Please try again.");
                return View(model);
            }

            // Fresh session, then store the token, role, name and user ID
            HttpContext.Session.Clear();
            HttpContext.Session.SetString(ApiClient.TokenKey, result.Data.Token);
            HttpContext.Session.SetString(RoleKey, result.Data.Role);
            HttpContext.Session.SetString(NameKey, result.Data.FullName);

            var userId = ReadUserIdFromToken(result.Data.Token);
            if (userId.HasValue)
            {
                HttpContext.Session.SetInt32(UserIdKey, userId.Value);
            }

            if (result.Data.Role == "Organiser")
                return RedirectToAction("Dashboard", "Organiser");

            return RedirectToAction("Index", "Home");
        }

        // ---------- REGISTER ----------

        [HttpGet]
        public IActionResult Register()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(ApiClient.TokenKey)))
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Organisers must enter the secret code
            if (model.Role == "Organiser" && string.IsNullOrWhiteSpace(model.OrganiserCode))
            {
                ModelState.AddModelError(nameof(model.OrganiserCode),
                    "Please enter the organiser code to register as an Organiser.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.PostAsync<RegisterResponseDto>("api/auth/register", new RegisterRequestDto
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role,
                OrganiserCode = model.Role == "Organiser" ? model.OrganiserCode : null
            });

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed. Please try again.");
                return View(model);
            }

            TempData["Success"] = "Your account has been created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // ---------- LOGOUT ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction(nameof(Login));
        }

        // ---------- Helper: read the user ID out of the JWT ----------

        private static int? ReadUserIdFromToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return null;

                // JWT parts are base64url, so convert to normal base64 first
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);

                foreach (var name in new[]
                {
                    "nameid",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                    "sub"
                })
                {
                    if (doc.RootElement.TryGetProperty(name, out var prop))
                    {
                        var text = prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
                        if (int.TryParse(text, out var id)) return id;
                    }
                }
            }
            catch
            {
                // If anything goes wrong we simply don't store the ID
            }

            return null;
        }
    }
}