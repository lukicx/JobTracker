namespace JobTracker.Api.Models;

public class JobApplication
{
    public int Id  { get; set; }
    public string Company { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Status { get; set; } = "Interested";

    public string? Location { get; set; }

    public string? JobUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}