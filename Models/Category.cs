namespace RaceDayAPI.Models;
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = ""; // e.g. 18-25, 26-35
    public int EventId { get; set; }
}
