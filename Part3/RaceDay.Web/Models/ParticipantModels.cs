namespace RaceDay.Web.Models
{
    public class EventDetailsViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();

        // Categories this logged-in Participant is already enrolled in
        public HashSet<int> EnrolledCategoryIds { get; set; } = new();

        public bool IsLoggedIn { get; set; }
        public bool IsParticipant { get; set; }
        public bool IsPast { get; set; }
    }

    // One row on the My Enrolments page
    public class MyEnrolmentRow
    {
        public EnrolmentDto Enrolment { get; set; } = new();
        public bool IsPast { get; set; }
    }

    // One row on the My Results page
    public class MyResultRow
    {
        public ResultDto Result { get; set; } = new();
        public DateTime? EventDate { get; set; }
        public int TotalFinishers { get; set; }
    }
}