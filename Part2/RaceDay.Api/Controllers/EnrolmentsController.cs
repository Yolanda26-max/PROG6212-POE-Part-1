using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // The user ID printed on the JWT wristband
    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private static readonly Expression<Func<Enrolment, EnrolmentResponse>> ToResponse = e => new EnrolmentResponse
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant!.FullName,
        CategoryId = e.CategoryId,
        CategoryName = e.Category!.CategoryName,
        EventId = e.Category!.EventId,
        EventName = e.Category!.Event!.EventName,
        EventDate = e.Category!.Event!.EventDate,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };

    /// <summary>
    /// Enrols the logged-in Participant into a category.
    /// </summary>
    /// <response code="201">Enrolled. Returns the new enrolment.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">The user is not a Participant.</response>
    /// <response code="404">The category does not exist.</response>
    /// <response code="409">Already enrolled in this category.</response>
    [HttpPost("categories/{categoryId:int}/enrol")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == categoryId))
        {
            return NotFound(new { message = "Category not found." });
        }

        var userId = CurrentUserId;

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == userId && e.CategoryId == categoryId))
        {
            return Conflict(new { message = "You are already enrolled in this category." });
        }

        var enrolment = new Enrolment { ParticipantId = userId, CategoryId = categoryId };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var response = await _db.Enrolments
            .Where(e => e.EnrolmentId == enrolment.EnrolmentId)
            .Select(ToResponse)
            .FirstAsync();

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Returns all enrolments belonging to the logged-in Participant.
    /// </summary>
    /// <response code="200">Returns the list of enrolments (may be empty).</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">The user is not a Participant.</response>
    [HttpGet("users/me/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = CurrentUserId;

        var list = await _db.Enrolments
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Returns all enrolments for an event. Only the Organiser who owns the event can do this.
    /// </summary>
    /// <response code="200">Returns the list of enrolments (may be empty).</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });
        }

        var list = await _db.Enrolments
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Cancels the logged-in Participant's own enrolment.
    /// </summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">Not a Participant, or not the owner of this enrolment.</response>
    /// <response code="404">The enrolment does not exist.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.ParticipantId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only cancel your own enrolments." });
        }

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}