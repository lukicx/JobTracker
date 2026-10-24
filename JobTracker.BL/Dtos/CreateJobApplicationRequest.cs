using System.ComponentModel.DataAnnotations;
using JobTracker.DAL.Models;

namespace JobTracker.BL.Dtos;

public class CreateJobApplicationRequest
{
    [Required]
    [MaxLength(JobApplicationLimits.TextMaxLength)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(JobApplicationLimits.TextMaxLength)]
    public string Position { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Interested;

    [MaxLength(JobApplicationLimits.TextMaxLength)]
    public string? Location { get; set; }

    [Url]
    public string? JobUrl { get; set; }
}