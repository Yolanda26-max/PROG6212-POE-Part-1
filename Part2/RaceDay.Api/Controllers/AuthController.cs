using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    /// <summary>
    /// Registers a new user as an Organiser or a Participant.
    /// Organisers must also supply the secret organiser code.
    /// </summary>
    /// <response code="201">User created. Returns the new user ID.</response>
    /// <response code="400">Invalid data or invalid role.</response>
    /// <response code="403">Organiser code missing or incorrect.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Work out which role was asked for
        string roleName;
        if (string.Equals(request.Role, "Organiser", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Organiser";
        }
        else if (string.Equals(request.Role, "Participant", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Participant";
        }
        else
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        // Anyone can be a Participant, but Organisers need the secret code
        if (roleName == "Organiser")
        {
            var expectedCode = _config["OrganiserRegistration:Code"];
            if (string.IsNullOrEmpty(expectedCode) || request.OrganiserCode != expectedCode)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "A valid organiser code is required to register as an Organiser." });
            }
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "That email address is already registered." });
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
        {
            return BadRequest(new { message = "Role does not exist." });
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            // The password is hashed here, so the plain password is never stored
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.RoleId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse { UserId = user.UserId });
    }

    /// <summary>
    /// Logs a user in and returns a JWT token plus their role.
    /// </summary>
    /// <response code="200">Login successful. Returns the token and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so attackers learn nothing
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roleName = user.Role!.RoleName;
        var (token, expiresAt) = _tokenService.CreateToken(user, roleName);

        return Ok(new AuthResponse
        {
            Token = token,
            Role = roleName,
            FullName = user.FullName,
            ExpiresAt = expiresAt
        });
    }
}