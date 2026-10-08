using System.ComponentModel.DataAnnotations;
using JobTracker.DAL.Models;

namespace JobTracker.BL.Dtos;

public class CreateJobApplicationRequest
{
    [Required]
    [MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Position { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Interested;

    [MaxLength(200)]
    public string? Location { get; set; }

    [Url]
    public string? JobUrl { get; set; }
}