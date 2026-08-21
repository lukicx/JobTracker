using JobTracker.Api.Data;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ApplicationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<JobApplication>>> GetAll()
    {
        var applications = await _db.Applications.ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobApplication>> GetById(int id)
    {
        var application = await _db.Applications.FindAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(application);
    }

    [HttpPost]
    public async Task<ActionResult<JobApplication>> Create(
        JobApplication application)
    {
        application.CreatedAt = DateTime.UtcNow;

        _db.Applications.Add(application);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = application.Id },
            application
        );
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var application = await _db.Applications.FindAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        _db.Applications.Remove(application);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, JobApplication updated)
    {
        var application = await _db.Applications.FindAsync(id);
        if (application is null)
        {
            return NotFound();
        }
        application.Company = updated.Company;
        application.Position = updated.Position;
        application.Status = updated.Status;
        application.Location = updated.Location;
        application.JobUrl = updated.JobUrl;
        
        await _db.SaveChangesAsync();
        return NoContent();
        
    }

   
}