using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [SessionAuthorize("Participant")]
    public class ParticipantController : Controller
    {
        private readonly ApiClient _api;

        public ParticipantController(ApiClient api)
        {
            _api = api;
        }

        // ---------- MY ENROLMENTS ----------

        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            var result = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");

            if (!result.Success)
            {
                TempData["Error"] = result.Error ?? "We couldn't load your enrolments.";
            }

            var rows = (result.Data ?? new List<EnrolmentDto>())
                .OrderBy(e => e.EventDate)
                .Select(e => new MyEnrolmentRow
                {
                    Enrolment = e,
                    IsPast = e.EventDate.Date < DateTime.Today
                })
                .ToList();

            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var result = await _api.DeleteAsync<object>($"api/enrolments/{id}");

            if (result.Success)
                TempData["Success"] = "Your enrolment was cancelled.";
            else
                TempData["Error"] = result.Error ?? "We couldn't cancel that enrolment.";

            return RedirectToAction(nameof(MyEnrolments));
        }

        // ---------- MY RESULTS ----------

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var result = await _api.GetAsync<List<ResultDto>>("api/users/me/results");

            if (!result.Success)
            {
                TempData["Error"] = result.Error ?? "We couldn't load your results.";
                return View(new List<MyResultRow>());
            }

            var results = result.Data ?? new List<ResultDto>();

            // Look up each event once: its date, and how many people finished
            var eventDates = new Dictionary<int, DateTime?>();
            var finisherCounts = new Dictionary<int, int>();

            foreach (var eventId in results.Select(r => r.EventId).Distinct())
            {
                var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
                eventDates[eventId] = ev.Data?.EventDate;

                var all = await _api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
                finisherCounts[eventId] = all.Data?.Count(r => r.Status == "Finished") ?? 0;
            }

            var rows = results
                .Select(r => new MyResultRow
                {
                    Result = r,
                    EventDate = eventDates.GetValueOrDefault(r.EventId),
                    TotalFinishers = finisherCounts.GetValueOrDefault(r.EventId)
                })
                .OrderByDescending(r => r.EventDate)
                .ToList();

            return View(rows);
        }
    }
}