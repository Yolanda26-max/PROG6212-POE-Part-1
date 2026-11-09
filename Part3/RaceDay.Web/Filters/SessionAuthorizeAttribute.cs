using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters
{
    /// <summary>
    /// Put this on a controller or action to protect it.
    ///   [SessionAuthorize]                 -> any logged-in user
    ///   [SessionAuthorize("Organiser")]    -> Organisers only
    ///   [SessionAuthorize("Participant")]  -> Participants only
    /// Think of it as a security guard at the door who checks the session.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string? _role;

        public SessionAuthorizeAttribute(string? role = null)
        {
            _role = role;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var token = session.GetString(ApiClient.TokenKey);
            var userRole = session.GetString("Role");

            var controller = context.Controller as Controller;

            // Not logged in -> send to the login page
            if (string.IsNullOrEmpty(token))
            {
                if (controller != null)
                {
                    controller.TempData["Error"] = "Please log in to continue.";
                }

                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Logged in, but the wrong role -> send back to their own home
            if (!string.IsNullOrEmpty(_role) &&
                !string.Equals(userRole, _role, StringComparison.OrdinalIgnoreCase))
            {
                if (controller != null)
                {
                    controller.TempData["Error"] = "You don't have access to that page.";
                }

                context.Result = userRole == "Organiser"
                    ? new RedirectToActionResult("Dashboard", "Organiser", null)
                    : new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}