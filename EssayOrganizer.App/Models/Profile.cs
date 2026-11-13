namespace EssayOrganizer.App.Models;

public class Profile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Either a built-in SVG name or the copied local image path.
    public string Avatar { get; set; } = "violet";
    public DateTime CreatedAt { get; set; }
}
