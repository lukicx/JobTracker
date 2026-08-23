using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Dtos;

public class CreateJobApplicationRequest
{
    [Required]
    [MaxLength(50)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Position { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Interested";

    [MaxLength(50)]
    public string? Location { get; set; }

    [Url]
    public string? JobUrl { get; set; }
}