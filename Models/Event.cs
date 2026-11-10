namespace RaceDayWeb.Models;
public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public string Location { get; set; } = "";
    public double Distance { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
    public int OrganiserId { get; set; }
    public string? BannerImage { get; set; }
}
