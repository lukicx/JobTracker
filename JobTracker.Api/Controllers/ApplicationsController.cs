using JobTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private static readonly List<JobApplication> Applications =
    [
        new JobApplication
        {
            Id = 1,
            Company = "Blah blah blah",
            Position = "Software Engineering Intern",
            Status = "Interested",
            Location = "Brno"
        }
    ];

    [HttpGet]
    public ActionResult<List<JobApplication>> GetAll()
    {
        return Ok(Applications);
    }
    [HttpGet("{id}")]
    public ActionResult<JobApplication> GetById(int id)
    {
        var application = Applications.FirstOrDefault(x => x.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(application);
    }
    [HttpPost]
    public ActionResult<JobApplication> Create(JobApplication application)
    {
        application.Id = Applications.Count + 1;
        application.CreatedAt = DateTime.UtcNow;

        Applications.Add(application);

        return CreatedAtAction(
            nameof(GetById),
            new { id = application.Id },
            application
        );
    }
}

