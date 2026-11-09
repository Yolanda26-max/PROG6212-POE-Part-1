using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // ---------- What the API sends back ----------

    public class EventDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventType { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string? BannerImageUrl { get; set; }
    }

    // ---------- What the Organiser form sends to the API ----------

    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter the event name.")]
        [MaxLength(150)]
        [Display(Name = "Event name")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose the event date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Event date")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

        [Required(ErrorMessage = "Please enter the location.")]
        [MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose an event type.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        [Required(ErrorMessage = "Please enter the distance.")]
        [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        // The image the Organiser picks (optional)
        [Display(Name = "Banner image")]
        public IFormFile? BannerImage { get; set; }

        // The banner already saved for this event (shown on the edit page)
        public string? ExistingBannerUrl { get; set; }
    }

    public class EventRequestDto
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventType { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
    }

    // ---------- Home page (list + filters) ----------

    public class HomeIndexViewModel
    {
        public List<EventDto> Events { get; set; } = new();

        // Current filter values, so the form remembers what was chosen
        public string? Search { get; set; }
        public string? EventType { get; set; }
        public string? Distance { get; set; }

        public int TotalBeforeFilter { get; set; }
        public string? ErrorMessage { get; set; }
    }
}