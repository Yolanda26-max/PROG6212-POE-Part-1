using Microsoft.Extensions.Logging;

namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Each user belongs to one role (Roles 1 : M Users)
    public Role? Role { get; set; }

    // Events this user organises (Users 1 : M Events)
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

    // Enrolments this user has made as a participant (Users 1 : M Enrolments)
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}