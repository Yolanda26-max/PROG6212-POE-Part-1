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
public class ResultsController : ControllerBase
{
    private static readonly string[] ValidStatuses = { "Pending", "Finished", "DNF", "DNS" };

    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // The user ID printed on the JWT wristband
    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private static readonly Expression<Func<Result, ResultResponse>> ToResponse = r => new ResultResponse
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantName = r.Enrolment!.Participant!.FullName,
        CategoryId = r.Enrolment!.CategoryId,
        CategoryName = r.Enrolment!.Category!.CategoryName,
        EventId = r.Enrolment!.Category!.EventId,
        EventName = r.Enrolment!.Category!.Event!.EventName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };

    /// <summary>
    /// Captures (or updates) the result for an enrolment. Only the Organiser who owns the event can do this.
    /// </summary>
    /// <response code="201">Result recorded for the first time.</response>
    /// <response code="200">Existing result updated.</response>
    /// <response code="400">Invalid data (status must be Pending, Finished, DNF or DNS; Finished needs a finish time).</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The enrolment does not exist.</response>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, [FromBody] ResultRequest request)
    {
        // Who organises the event this enrolment belongs to? (null = enrolment doesn't exist)
        var ownerId = await _db.Enrolments
            .Where(e => e.EnrolmentId == enrolmentId)
            .Select(e => (int?)e.Category!.Event!.OrganiserId)
            .FirstOrDefaultAsync();

        if (ownerId == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (ownerId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for your own events." });
        }

        var status = ValidStatuses.FirstOrDefault(s =>
            string.Equals(s, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (status == null)
        {
            return BadRequest(new { message = "Status must be 'Pending', 'Finished', 'DNF' or 'DNS'." });
        }

        if (status == "Finished" && request.FinishTime == null)
        {
            return BadRequest(new { message = "A finish time is required when the status is 'Finished'." });
        }

        var result = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);
        var isNew = result == null;

        if (result == null)
        {
            result = new Result { EnrolmentId = enrolmentId };
            _db.Results.Add(result);
        }

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = status;

        await _db.SaveChangesAsync();

        var response = await _db.Results
            .Where(r => r.ResultId == result.ResultId)
            .Select(ToResponse)
            .FirstAsync();

        return StatusCode(isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Returns the logged-in Participant's own results.
    /// </summary>
    /// <response code="200">Returns the list of results (may be empty).</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="403">The user is not a Participant.</response>
    [HttpGet("users/me/results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = CurrentUserId;

        var list = await _db.Results
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Returns the results (leaderboard) for an event, best position first. Public, no login needed.
    /// </summary>
    /// <response code="200">Returns the list of results (may be empty).</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("events/{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var list = await _db.Results
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.Position == null ? 1 : 0)
            .ThenBy(r => r.Position)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(list);
    }
}