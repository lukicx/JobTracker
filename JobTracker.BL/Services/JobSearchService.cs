using System.Text.Json;
using JobTracker.DAL.Models;
using JobTracker.BL.Dtos;
using JobTracker.DAL.Providers;
using Microsoft.Extensions.Logging;

namespace JobTracker.BL.Services;

public class JobSearchService
{
    private readonly JoobleJobProvider _jooble;
    private readonly JobsCzJobProvider _jobsCz;
    private readonly ILogger<JobSearchService> _logger;

    public JobSearchService(JoobleJobProvider jooble, JobsCzJobProvider jobsCz, ILogger<JobSearchService> logger)
    {
        _jooble = jooble;
        _jobsCz = jobsCz;
        _logger = logger;
    }

    public async Task<(JobSearchResponse Result, List<string> Warnings, int ProviderCount)> SearchJobsAsync(
        string keywords, string location, string source, int jobsCzPage, int jooblePage)
    {
        var requests = new List<Task<(string Source, List<ExternalJob> Jobs, int? NextPage, string? Warning)>>();
        if (jobsCzPage > 0 && source is ("all" or "jobscz"))
        {
            requests.Add(SearchProviderAsync("Jobs.cz", jobsCzPage,
                () => _jobsCz.SearchJobsAsync(keywords, location, jobsCzPage)));
        }
        if (jooblePage > 0 && source is ("all" or "jooble"))
        {
            requests.Add(SearchProviderAsync("Jooble", jooblePage,
                () => _jooble.SearchJobsAsync(keywords, location, jooblePage)));
        }

        var searches = await Task.WhenAll(requests);

        return (
            new JobSearchResponse
            {
                Jobs = searches.SelectMany(result => result.Jobs).DistinctBy(job => job.ExternalId).ToList(),
                NextJobsCzPage = searches.FirstOrDefault(result => result.Source == "Jobs.cz").NextPage,
                NextJooblePage = searches.FirstOrDefault(result => result.Source == "Jooble").NextPage
            },
            searches.Where(result => result.Warning is not null).Select(result => result.Warning!).ToList(),
            requests.Count);
    }

    private async Task<(string Source, List<ExternalJob> Jobs, int? NextPage, string? Warning)> SearchProviderAsync(
        string source, int page, Func<Task<(List<ExternalJob> Jobs, bool HasMore)>> search)
    {
        try
        {
            var (jobs, hasMore) = await search();
            return (source, jobs, hasMore ? page + 1 : null, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                                          or InvalidOperationException or JsonException)
        {
            // Do not log request URLs: Jooble puts the API key in its URL.
            _logger.LogWarning("{Source} job search failed ({ErrorType}).", source, exception.GetType().Name);
            return (source, [], page, $"{source} is currently unavailable. Please try again later.");
        }
    }
}
