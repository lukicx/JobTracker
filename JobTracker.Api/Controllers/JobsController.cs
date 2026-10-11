using JobTracker.DAL.Models;
using JobTracker.BL.Services;
using JobTracker.BL.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly JobSearchService _jobSearch;

    public JobsController(JobSearchService jobSearch)
    {
        _jobSearch = jobSearch;
    }

    [HttpGet]
    public async Task<ActionResult<JobSearchResponse>> SearchJobs(
        string search, string location, string source = "all", int page = 1,
        int? jobsCzPage = null, int? jooblePage = null)
    {
        source = source.Trim().ToLowerInvariant();
        if (source is not ("all" or "jobscz" or "jooble"))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "Source must be all, jobscz, or jooble.");
        }

        if (page < 1 || jobsCzPage < 0 || jooblePage < 0
            || page == int.MaxValue || jobsCzPage == int.MaxValue || jooblePage == int.MaxValue)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "Page must be positive. Provider pages must be positive or 0 to skip a finished source.");
        }

        var (result, warnings, providerCount) = await _jobSearch.SearchJobsAsync(
            search, location, source, jobsCzPage ?? page, jooblePage ?? page);

        if (providerCount == 0)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "At least one selected source must have a page to fetch.");
        }

        if (warnings.Count == providerCount)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway,
                detail: string.Join(" ", warnings));
        }

        if (warnings.Count > 0)
        {
            Response.Headers["X-Job-Search-Warning"] = string.Join(" ", warnings);
        }

        return Ok(result);
    }
}
