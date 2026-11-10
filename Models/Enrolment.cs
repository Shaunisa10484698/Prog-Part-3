namespace RaceDayAPI.Models;
public class Enrolment
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public DateTime EnrolledDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Confirmed";
}