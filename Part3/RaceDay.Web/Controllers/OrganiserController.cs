using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [SessionAuthorize("Organiser")]
    public class OrganiserController : Controller
    {
        private readonly ApiClient _api;

        public OrganiserController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32(AccountController.UserIdKey);

            // Sessions created before we saved the ID need a fresh login
            if (userId == null)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Please log in again to continue.";
                return RedirectToAction("Login", "Account");
            }

            var model = new OrganiserDashboardViewModel
            {
                OrganiserName = HttpContext.Session.GetString(AccountController.NameKey) ?? "Organiser"
            };

            // includePast=true so the Organiser sees every event they have created
            var eventsResult = await _api.GetAsync<List<EventDto>>("api/events?includePast=true");

            if (!eventsResult.Success || eventsResult.Data == null)
            {
                model.ErrorMessage = eventsResult.Error ?? "We couldn't load your events right now.";
                return View(model);
            }

            var myEvents = eventsResult.Data
                .Where(e => e.OrganiserId == userId.Value)
                .OrderBy(e => e.EventDate)
                .ToList();

            foreach (var ev in myEvents)
            {
                var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{ev.EventId}/enrolments");
                var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventId}/categories");

                model.Events.Add(new DashboardEventRow
                {
                    Event = ev,
                    EnrolmentCount = enrolments.Data?.Count ?? 0,
                    CategoryCount = categories.Data?.Count ?? 0
                });
            }

            model.TotalEvents = model.Events.Count;
            model.TotalEnrolments = model.Events.Sum(r => r.EnrolmentCount);
            model.UpcomingEvents = model.Events.Count(r => r.Event.EventDate.Date >= DateTime.Today);

            return View(model);
        }
    }
}