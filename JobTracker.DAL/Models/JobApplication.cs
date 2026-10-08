using System.ComponentModel.DataAnnotations;

namespace JobTracker.DAL.Models;

public class JobApplication
{
    public int Id  { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Interested;
    public string? Location { get; set; }
    public string? JobUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;

}