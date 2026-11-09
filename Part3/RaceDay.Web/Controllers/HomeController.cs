using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _api;

        public HomeController(ApiClient api)
        {
            _api = api;
        }

        // Upcoming events with filters (search, type, distance)
        public async Task<IActionResult> Index(string? search, string? eventType, string? distance)
        {
            var model = new HomeIndexViewModel
            {
                Search = search,
                EventType = eventType,
                Distance = distance
            };

            var result = await _api.GetAsync<List<EventDto>>("api/events");

            if (!result.Success || result.Data == null)
            {
                model.ErrorMessage = result.Error ?? "We couldn't load the events right now.";
                return View(model);
            }

            var events = result.Data;
            model.TotalBeforeFilter = events.Count;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                events = events.Where(e =>
                    e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Location.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                events = events.Where(e =>
                    string.Equals(e.EventType, eventType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(distance))
            {
                events = distance switch
                {
                    "short" => events.Where(e => e.DistanceKm <= 5m).ToList(),
                    "medium" => events.Where(e => e.DistanceKm > 5m && e.DistanceKm <= 10m).ToList(),
                    "long" => events.Where(e => e.DistanceKm > 10m && e.DistanceKm <= 21.1m).ToList(),
                    "ultra" => events.Where(e => e.DistanceKm > 21.1m).ToList(),
                    _ => events
                };
            }

            model.Events = events.OrderBy(e => e.EventDate).ToList();
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}