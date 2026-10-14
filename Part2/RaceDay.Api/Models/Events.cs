namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Added for Part 2 (required by the brief): "Run", "Walk" or "Cycle"
    public string EventType { get; set; } = string.Empty;

    // Added for Part 2 (required by the brief): overall event distance in km
    public decimal DistanceKm { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The organiser who owns this event (Users 1 : M Events)
    public User? Organiser { get; set; }

    // Categories offered by this event (Events 1 : M Categories)
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}