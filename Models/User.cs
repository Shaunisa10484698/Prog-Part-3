namespace RaceDayAPI.Models;
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = ""; // Organiser or Participant
    public string? ProfileImageUrl { get; set; }

    public string? ProfilePicture { get; set; }
}