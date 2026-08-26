using JobTracker.Api.Models;

namespace JobTracker.Api.Dtos;

public class UpdateApplicationStatus
{
    public ApplicationStatus Status { get; set; }
}