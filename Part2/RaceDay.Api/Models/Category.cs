namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal Price { get; set; }

    // The event this category belongs to (Events 1 : M Categories)
    public Event? Event { get; set; }

    // Enrolments made into this category (Categories 1 : M Enrolments)
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}