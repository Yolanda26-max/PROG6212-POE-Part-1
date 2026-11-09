using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // ---------- Categories ----------

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal Price { get; set; }
    }

    public class CategoryRequestDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal Price { get; set; }
    }

    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter a category name.")]
        [MaxLength(100)]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the distance.")]
        [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Required(ErrorMessage = "Please enter the entry price (use 0 for free).")]
        [Range(0, 999999.99, ErrorMessage = "Price cannot be negative.")]
        [Display(Name = "Entry price (R)")]
        public decimal Price { get; set; }
    }

    public class ManageCategoriesViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public CategoryFormViewModel NewCategory { get; set; } = new();
    }

    // ---------- Enrolments ----------

    public class EnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class EventEnrolmentsViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<EnrolmentDto> Enrolments { get; set; } = new();
    }

    // ---------- Results ----------

    public class ResultDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ResultRequestDto
    {
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // One row on the results entry page: an enrolment plus its result (if any)
    public class ResultRowViewModel
    {
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Finish time (hh:mm:ss)")]
        public string? FinishTime { get; set; }

        [Range(1, 100000, ErrorMessage = "Position must be 1 or more.")]
        public int? Position { get; set; }

        public string Status { get; set; } = "Pending";
    }

    public class ResultsEntryViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<ResultRowViewModel> Rows { get; set; } = new();
    }

    // ---------- Organiser dashboard ----------

    public class DashboardEventRow
    {
        public EventDto Event { get; set; } = new();
        public int EnrolmentCount { get; set; }
        public int CategoryCount { get; set; }
    }

    public class OrganiserDashboardViewModel
    {
        public string OrganiserName { get; set; } = string.Empty;
        public List<DashboardEventRow> Events { get; set; } = new();
        public int TotalEvents { get; set; }
        public int TotalEnrolments { get; set; }
        public int UpcomingEvents { get; set; }
        public string? ErrorMessage { get; set; }
    }
}