using JobTracker.DAL.Models;

namespace JobTracker.BL.Dtos;

public class UpdateApplicationStatus
{
    public ApplicationStatus Status { get; set; }
}