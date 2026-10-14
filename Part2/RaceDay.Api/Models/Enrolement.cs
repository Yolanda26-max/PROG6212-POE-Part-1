namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Confirmed";

    // The participant who enrolled (Users 1 : M Enrolments)
    public User? Participant { get; set; }

    // The category they enrolled into (Categories 1 : M Enrolments)
    public Category? Category { get; set; }

    // The result for this enrolment, once the race is done (Enrolments 1 : 1 Results)
    public Result? Result { get; set; }
}