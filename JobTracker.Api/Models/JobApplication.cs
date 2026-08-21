using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Models;

public class JobApplication
{
    public int Id  { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Position { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Interested";
    [MaxLength(50)]
    public string? Location { get; set; }

    public string? JobUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}