using System.ComponentModel.DataAnnotations;
using JobTracker.DAL.Models;

namespace JobTracker.BL.Dtos;

public class UpdateJobApplicationRequest
{
    [Required]
    [MaxLength(50)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Position { get; set; } = string.Empty;

    [Required] 
    public ApplicationStatus Status { get; set; }

    [MaxLength(50)]
    public string? Location { get; set; }

    [Url]
    public string? JobUrl { get; set; }
}