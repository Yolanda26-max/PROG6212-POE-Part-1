using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    // The user ID printed on the JWT wristband
    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>
    /// Returns the profile of the currently logged-in user (Organiser or Participant).
    /// </summary>
    /// <response code="200">Returns the user's profile.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="404">The user no longer exists.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = CurrentUserId;

        var profile = await _db.Users
            .Where(u => u.UserId == userId)
            .Select(u => new UserProfileResponse
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role!.RoleName,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Updates the full name and email of the currently logged-in user.
    /// </summary>
    /// <response code="200">Profile updated. Returns the new profile.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token supplied.</response>
    /// <response code="404">The user no longer exists.</response>
    /// <response code="409">That email belongs to another user.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = CurrentUserId;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // The new email must not belong to someone else
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
        {
            return Conflict(new { message = "That email address is already registered." });
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        var roleName = await _db.Roles
            .Where(r => r.RoleId == user.RoleId)
            .Select(r => r.RoleName)
            .FirstAsync();

        return Ok(new UserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName,
            CreatedAt = user.CreatedAt
        });
    }
}