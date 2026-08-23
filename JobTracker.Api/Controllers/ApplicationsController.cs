using JobTracker.Api.Data;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Api.Dtos;

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
    private static JobApplicationResponse ToResponse(JobApplication application)
    {
        return new JobApplicationResponse
        {
            Id = application.Id,
            Company = application.Company,
            Position = application.Position,
            Status = application.Status,
            Location = application.Location,
            JobUrl = application.JobUrl,
            CreatedAt = application.CreatedAt
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<JobApplicationResponse>>> GetAll(ApplicationStatus? status, string? search)
    {
        var query = _db.Applications.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(application =>
                application.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            query = query.Where(application =>
                EF.Functions.ILike(application.Company, pattern) ||
                EF.Functions.ILike(application.Position, pattern) ||
                (application.Location != null &&
                 EF.Functions.ILike(application.Location, pattern)));
        }

        var applications = await query
            .OrderByDescending(application => application.CreatedAt)
            .ToListAsync();

        return Ok(applications.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobApplicationResponse>> GetById(int id)
    {
        var application = await _db.Applications.FindAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(application));
    }

    [HttpPost]
    public async Task<ActionResult<JobApplicationResponse>> Create(CreateJobApplicationRequest request)
    {
        var application = new JobApplication
        {
            Company = request.Company,
            Position = request.Position,
            Status = request.Status,
            Location = request.Location,
            JobUrl = request.JobUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        var response = ToResponse(application);

        return CreatedAtAction(
            nameof(GetById),
            new { id = application.Id },
            response
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
    public async Task<IActionResult> Update(int id,  UpdateJobApplicationRequest request)
    {
        var application = await _db.Applications.FindAsync(id);
        if (application is null)
        {
            return NotFound();
        }
        application.Company = request.Company;
        application.Position = request.Position;
        application.Status = request.Status;
        application.Location = request.Location;
        application.JobUrl = request.JobUrl;
        
        await _db.SaveChangesAsync();
        return NoContent();
        
    }

   
}