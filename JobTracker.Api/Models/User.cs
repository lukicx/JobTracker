using Microsoft.AspNetCore.Identity;

namespace JobTracker.Api.Models;

public class User : IdentityUser
{
    public ICollection<JobApplication> Applications { get; set; } = [];
}