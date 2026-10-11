using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using JobTracker.DAL.Dtos;
using JobTracker.DAL.Models;

namespace JobTracker.DAL.Providers;

public class JoobleJobProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public JoobleJobProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        _apiKey = configuration["Jooble:ApiKey"] ?? string.Empty;
    }

    public async Task<(List<ExternalJob> Jobs, bool HasMore)> SearchJobsAsync(
        string keywords, string location, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Jooble API key is not configured.");
        }

        var request = new JoobleSearchRequest
        {
            Keywords = keywords,
            Location = location,
            Page = page
        };

        using var response = await _httpClient.PostAsJsonAsync($"https://cz.jooble.org/api/{_apiKey}", request);

        response.EnsureSuccessStatusCode();

        var joobleResponse =
            await response.Content.ReadFromJsonAsync<JoobleSearchResponse>();

        if (joobleResponse is null)
        {
            throw new InvalidOperationException("Jooble did not return a recognized search response.");
        }

        var jobs = joobleResponse.Jobs
            .Select(job => new ExternalJob
            {
                ExternalId = $"jooble:{job.Id}",
                Company = job.Company,
                Position = job.Title,
                Location = job.Location,
                Url = job.Link,
                Description = job.Snippet
            })
            .ToList();

        var hasMore = jobs.Count > 0 && (long)page * request.ResultOnPage < joobleResponse.TotalCount;
        return (jobs, hasMore);
    }
}
