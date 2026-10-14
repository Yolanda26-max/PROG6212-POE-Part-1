namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }

    // SQL TIME maps to TimeSpan in C#. Null until the organiser captures it.
    public TimeSpan? FinishTime { get; set; }

    // Null until the organiser captures it
    public int? Position { get; set; }

    public string Status { get; set; } = "Pending";

    // The enrolment this result belongs to (Enrolments 1 : 1 Results)
    public Enrolment? Enrolment { get; set; }
}