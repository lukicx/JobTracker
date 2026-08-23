using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Models;

namespace JobTracker.Api.Dtos;

public class CreateJobApplicationRequest
{
    [Required]
    [MaxLength(50)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Position { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Interested;

    [MaxLength(50)]
    public string? Location { get; set; }

    [Url]
    public string? JobUrl { get; set; }
}