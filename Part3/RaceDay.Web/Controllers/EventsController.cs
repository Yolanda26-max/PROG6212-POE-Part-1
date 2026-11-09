using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApiClient _api;

        public EventsController(ApiClient api)
        {
            _api = api;
        }

        // Public: anyone can look at an event
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var eventResult = await _api.GetAsync<EventDto>($"api/events/{id}");

            if (!eventResult.Success || eventResult.Data == null)
            {
                TempData["Error"] = eventResult.Error ?? "We couldn't find that event.";
                return RedirectToAction("Index", "Home");
            }

            var categoriesResult = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");

            var role = HttpContext.Session.GetString(AccountController.RoleKey);
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString(ApiClient.TokenKey));

            var model = new EventDetailsViewModel
            {
                Event = eventResult.Data,
                Categories = categoriesResult.Data ?? new List<CategoryDto>(),
                IsLoggedIn = isLoggedIn,
                IsParticipant = isLoggedIn && role == "Participant",
                IsPast = eventResult.Data.EventDate.Date < DateTime.Today
            };

            // Work out which categories this Participant has already joined
            if (model.IsParticipant)
            {
                var mine = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
                if (mine.Success && mine.Data != null)
                {
                    model.EnrolledCategoryIds = mine.Data
                        .Where(e => e.EventId == id)
                        .Select(e => e.CategoryId)
                        .ToHashSet();
                }
            }

            return View(model);
        }

        // Participants only: enrol in a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Participant")]
        public async Task<IActionResult> Enrol(int eventId, int categoryId, string categoryName)
        {
            var result = await _api.PostAsync<EnrolmentDto>($"api/categories/{categoryId}/enrol");

            if (result.Success)
                TempData["Success"] = $"You're enrolled in {categoryName}. Good luck!";
            else
                TempData["Error"] = result.Error ?? "We couldn't enrol you. Please try again.";

            return RedirectToAction(nameof(Details), new { id = eventId });
        }
    }
}