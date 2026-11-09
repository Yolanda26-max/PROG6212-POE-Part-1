using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [SessionAuthorize("Organiser")]
    public class OrganiserEventsController : Controller
    {
        private readonly ApiClient _api;

        public OrganiserEventsController(ApiClient api)
        {
            _api = api;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32(AccountController.UserIdKey);

        // ---------- LIST (My Events) ----------

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Please log in again to continue.";
                return RedirectToAction("Login", "Account");
            }

            var result = await _api.GetAsync<List<EventDto>>("api/events?includePast=true");

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "We couldn't load your events.";
                return View(new List<EventDto>());
            }

            var mine = result.Data
                .Where(e => e.OrganiserId == userId.Value)
                .OrderBy(e => e.EventDate)
                .ToList();

            return View(mine);
        }

        // ---------- CREATE ----------

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.PostAsync<EventDto>("api/events", ToRequest(model));

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't create the event.");
                return View(model);
            }

            TempData["Success"] = $"Event \"{model.EventName}\" was created. You can now add categories to it.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- EDIT ----------

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _api.GetAsync<EventDto>($"api/events/{id}");

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "We couldn't find that event.";
                return RedirectToAction(nameof(Index));
            }

            if (result.Data.OrganiserId != CurrentUserId)
            {
                TempData["Error"] = "You can only edit your own events.";
                return RedirectToAction(nameof(Index));
            }

            var e = result.Data;
            return View(new EventFormViewModel
            {
                EventId = e.EventId,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                Description = e.Description,
                EventType = e.EventType,
                DistanceKm = e.DistanceKm
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            model.EventId = id;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model));

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't save your changes.");
                return View(model);
            }

            TempData["Success"] = $"Event \"{model.EventName}\" was updated.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- DELETE ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _api.DeleteAsync<object>($"api/events/{id}");

            if (result.Success)
                TempData["Success"] = "The event was deleted.";
            else
                TempData["Error"] = result.Error ?? "We couldn't delete that event.";

            return RedirectToAction(nameof(Index));
        }

        // ---------- CATEGORIES ----------

        [HttpGet]
        public async Task<IActionResult> Categories(int id)
        {
            var ev = await LoadOwnedEventAsync(id);
            if (ev == null) return RedirectToAction(nameof(Index));

            var model = await BuildCategoriesModelAsync(ev);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int id, CategoryFormViewModel newCategory)
        {
            var ev = await LoadOwnedEventAsync(id);
            if (ev == null) return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildCategoriesModelAsync(ev);
                invalidModel.NewCategory = newCategory;
                return View(nameof(Categories), invalidModel);
            }

            var result = await _api.PostAsync<CategoryDto>($"api/events/{id}/categories", new CategoryRequestDto
            {
                CategoryName = newCategory.CategoryName,
                DistanceKm = newCategory.DistanceKm,
                Price = newCategory.Price
            });

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't add that category.");
                var failedModel = await BuildCategoriesModelAsync(ev);
                failedModel.NewCategory = newCategory;
                return View(nameof(Categories), failedModel);
            }

            TempData["Success"] = $"Category \"{newCategory.CategoryName}\" was added.";
            return RedirectToAction(nameof(Categories), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int eventId)
        {
            var result = await _api.DeleteAsync<object>($"api/categories/{id}");

            if (result.Success)
                TempData["Success"] = "The category was deleted.";
            else
                TempData["Error"] = result.Error ?? "We couldn't delete that category.";

            return RedirectToAction(nameof(Categories), new { id = eventId });
        }

        // ---------- ENROLMENTS (view only) ----------

        [HttpGet]
        public async Task<IActionResult> Enrolments(int id)
        {
            var ev = await LoadOwnedEventAsync(id);
            if (ev == null) return RedirectToAction(nameof(Index));

            var result = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments");

            if (!result.Success)
            {
                TempData["Error"] = result.Error ?? "We couldn't load the enrolments.";
            }

            return View(new EventEnrolmentsViewModel
            {
                Event = ev,
                Enrolments = result.Data ?? new List<EnrolmentDto>()
            });
        }

        // ---------- RESULTS ----------

        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            var ev = await LoadOwnedEventAsync(id);
            if (ev == null) return RedirectToAction(nameof(Index));

            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments");
            var results = await _api.GetAsync<List<ResultDto>>($"api/events/{id}/results");

            if (!enrolments.Success)
            {
                TempData["Error"] = enrolments.Error ?? "We couldn't load the participants.";
            }

            var existing = (results.Data ?? new List<ResultDto>())
                .ToDictionary(r => r.EnrolmentId);

            var rows = (enrolments.Data ?? new List<EnrolmentDto>())
                .OrderBy(e => e.CategoryName)
                .ThenBy(e => e.ParticipantName)
                .Select(e =>
                {
                    existing.TryGetValue(e.EnrolmentId, out var r);
                    return new ResultRowViewModel
                    {
                        EnrolmentId = e.EnrolmentId,
                        ParticipantName = e.ParticipantName,
                        CategoryName = e.CategoryName,
                        FinishTime = r?.FinishTime?.ToString(@"hh\:mm\:ss"),
                        Position = r?.Position,
                        Status = r?.Status ?? "Pending"
                    };
                })
                .ToList();

            return View(new ResultsEntryViewModel { Event = ev, Rows = rows });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(int eventId, ResultRowViewModel row)
        {
            TimeSpan? finishTime = null;

            if (!string.IsNullOrWhiteSpace(row.FinishTime))
            {
                if (!TimeSpan.TryParse(row.FinishTime.Trim(), out var parsed) || parsed < TimeSpan.Zero)
                {
                    TempData["Error"] = $"{row.ParticipantName}: please enter the finish time as hh:mm:ss, for example 01:45:32.";
                    return RedirectToAction(nameof(Results), new { id = eventId });
                }

                finishTime = parsed;
            }

            if (row.Status == "Finished" && finishTime == null)
            {
                TempData["Error"] = $"{row.ParticipantName}: a finish time is needed when the status is Finished.";
                return RedirectToAction(nameof(Results), new { id = eventId });
            }

            var result = await _api.PostAsync<ResultDto>($"api/enrolments/{row.EnrolmentId}/result", new ResultRequestDto
            {
                FinishTime = finishTime,
                Position = row.Position,
                Status = row.Status
            });

            if (result.Success)
                TempData["Success"] = $"Result saved for {row.ParticipantName}.";
            else
                TempData["Error"] = result.Error ?? "We couldn't save that result.";

            return RedirectToAction(nameof(Results), new { id = eventId });
        }

        // ---------- Helpers ----------

        // Loads an event and makes sure it belongs to the logged-in Organiser
        private async Task<EventDto?> LoadOwnedEventAsync(int id)
        {
            var result = await _api.GetAsync<EventDto>($"api/events/{id}");

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "We couldn't find that event.";
                return null;
            }

            if (result.Data.OrganiserId != CurrentUserId)
            {
                TempData["Error"] = "You can only manage your own events.";
                return null;
            }

            return result.Data;
        }

        private async Task<ManageCategoriesViewModel> BuildCategoriesModelAsync(EventDto ev)
        {
            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventId}/categories");

            return new ManageCategoriesViewModel
            {
                Event = ev,
                Categories = categories.Data ?? new List<CategoryDto>(),
                NewCategory = new CategoryFormViewModel { EventId = ev.EventId }
            };
        }

        private static EventRequestDto ToRequest(EventFormViewModel m) => new()
        {
            EventName = m.EventName,
            EventDate = m.EventDate,
            Location = m.Location,
            Description = m.Description,
            EventType = m.EventType,
            DistanceKm = m.DistanceKm
        };
    }
}