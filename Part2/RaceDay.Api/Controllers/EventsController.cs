using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] ValidEventTypes = { "Run", "Walk", "Cycle" };

    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // The user ID printed on the JWT wristband
    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // Accepts "run", "RUN" or "Run" and returns the proper "Run" (or null if invalid)
    private static string? NormaliseEventType(string? value) =>
        ValidEventTypes.FirstOrDefault(t =>
            string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static EventResponse ToResponse(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Description = e.Description,
        EventType = e.EventType,
        DistanceKm = e.DistanceKm
    };

    /// <summary>
    /// Lists upcoming events so participants can browse. Public, no login needed.
    /// </summary>
    /// <param name="includePast">Set to true to also include events that have already happened.</param>
    /// <response code="200">Returns the list of events, soonest first.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] bool includePast = false)
    {
        var query = _db.Events.Include(e => e.Organiser).AsQueryable();

        if (!includePast)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(e => e.EventDate >= today);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToResponse));
    }

    /// <summary>
    /// Returns the full detail of a single event. Public, no login needed.
    /// </summary>
    /// <response code="200">Returns the event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(ToResponse(ev));
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="201">Event created. Returns the new event, including its ID.</response>
    /// <response code="400">Invalid data, or event type is not Run, Walk or Cycle.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">The user is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var eventType = NormaliseEventType(request.EventType);
        if (eventType == null)
        {
            return BadRequest(new { message = "EventType must be 'Run', 'Walk' or 'Cycle'." });
        }

        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            EventType = eventType,
            DistanceKm = request.DistanceKm
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToResponse(ev));
    }

    /// <summary>
    /// Updates an event. Only the Organiser who created it can do this.
    /// </summary>
    /// <response code="200">Event updated. Returns the updated event.</response>
    /// <response code="400">Invalid data, or event type is not Run, Walk or Cycle.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change your own events." });
        }

        var eventType = NormaliseEventType(request.EventType);
        if (eventType == null)
        {
            return BadRequest(new { message = "EventType must be 'Run', 'Walk' or 'Cycle'." });
        }

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate.Date;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventType = eventType;
        ev.DistanceKm = request.DistanceKm;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>
    /// Deletes an event, including its categories, enrolments and results.
    /// Only the Organiser who created it can do this.
    /// </summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete your own events." });
        }

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}