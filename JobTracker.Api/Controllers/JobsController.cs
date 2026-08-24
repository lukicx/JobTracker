using JobTracker.Api.Models;
using JobTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly JoobleJobProvider _jobProvider;

    public JobsController(JoobleJobProvider jobProvider)
    {
        _jobProvider = jobProvider;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExternalJob>>> SearchJobs(string search, string location)
    {
        var jobs = await _jobProvider.SearchJobsAsync(search, location);

        return Ok(jobs);
    }
}