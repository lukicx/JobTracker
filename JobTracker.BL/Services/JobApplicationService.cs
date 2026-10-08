using JobTracker.BL.Dtos;
using JobTracker.DAL.Data;
using JobTracker.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.BL.Services;

public class JobApplicationService
{
    private readonly AppDbContext _db;

    public JobApplicationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<JobApplicationResponse>> GetAllAsync(
        string userId,
        ApplicationStatus? status,
        string? search)
    {
        var query = _db.Applications
            .Where(application => application.UserId == userId)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(application => application.Status == status.Value);
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
        return applications.Select(ToResponse).ToList();
    }

    public async Task<JobApplicationResponse?> GetByIdAsync(
        int id,
        string userId)
    {
        var application = await FindOwnedApplicationAsync(id, userId);
        return application is null ? null : ToResponse(application);
    }

    public async Task<JobApplicationResponse> CreateAsync(
        string userId,
        CreateJobApplicationRequest request)
    {
        var application = new JobApplication
        {
            Company = request.Company,
            Position = request.Position,
            Status = request.Status,
            Location = request.Location,
            JobUrl = request.JobUrl,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        return ToResponse(application);
    }

    public async Task<bool> UpdateAsync(
        int id,
        string userId,
        UpdateJobApplicationRequest request)
    {
        var application = await FindOwnedApplicationAsync(id, userId);
        if (application is null)
        {
            return false;
        }

        application.Company = request.Company;
        application.Position = request.Position;
        application.Status = request.Status;
        application.Location = request.Location;
        application.JobUrl = request.JobUrl;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        string userId,
        UpdateApplicationStatus request)
    {
        var application = await FindOwnedApplicationAsync(id, userId);
        if (application is null)
        {
            return false;
        }

        application.Status = request.Status;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        string userId)
    {
        var application = await FindOwnedApplicationAsync(id, userId);
        if (application is null)
        {
            return false;
        }

        _db.Applications.Remove(application);
        await _db.SaveChangesAsync();
        return true;
    }

    private Task<JobApplication?> FindOwnedApplicationAsync(
        int id,
        string userId)
    {
        return _db.Applications.FirstOrDefaultAsync(
            application => application.Id == id && application.UserId == userId);
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
}
