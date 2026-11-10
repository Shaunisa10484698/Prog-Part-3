namespace RaceDayAPI.Models;
public class Result
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int ParticipantId { get; set; }
    public string FinishTime { get; set; } = "";
    public int Position { get; set; }
}