using Microsoft.AspNetCore.Identity;

namespace JobTracker.DAL.Models;

public class User : IdentityUser
{
    public ICollection<JobApplication> Applications { get; set; } = [];
}