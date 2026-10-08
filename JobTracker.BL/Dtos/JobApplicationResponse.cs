using JobTracker.DAL.Models;

namespace JobTracker.BL.Dtos;

public class JobApplicationResponse
{
    public int Id { get; set; }

    public string Company { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Interested;

    public string? Location { get; set; }

    public string? JobUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}