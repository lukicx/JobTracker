using System.Security.Claims;
using JobTracker.BL.Dtos;
using JobTracker.BL.Services;
using JobTracker.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly JobApplicationService _applicationService;

    public ApplicationsController(JobApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<JobApplicationResponse>>> GetAll(
        ApplicationStatus? status,
        string? search)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var applications = await _applicationService.GetAllAsync(userId, status, search);
        return Ok(applications);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<JobApplicationResponse>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var application = await _applicationService.GetByIdAsync(id, userId);
        return application is null ? NotFound() : Ok(application);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<JobApplicationResponse>> Create(
        CreateJobApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var application = await _applicationService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateJobApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var updated = await _applicationService.UpdateAsync(id, userId, request);
        return updated ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateApplicationStatus request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var updated = await _applicationService.UpdateStatusAsync(id, userId, request);
        return updated ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var deleted = await _applicationService.DeleteAsync(id, userId);
        return deleted ? NoContent() : NotFound();
    }
}
