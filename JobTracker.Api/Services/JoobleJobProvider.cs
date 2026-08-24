using System.Net.Http.Json;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;

namespace JobTracker.Api.Services;

public class JoobleJobProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public JoobleJobProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        _apiKey = configuration["Jooble:ApiKey"] ?? throw new InvalidOperationException("Jooble API key is not configured.");
    }

    public async Task<List<ExternalJob>> SearchJobsAsync(string keywords, string location)
    {
        var request = new JoobleSearchRequest
        {
            Keywords = keywords,
            Location = location,
            Page = 1
        };

        var response = await _httpClient.PostAsJsonAsync($"https://cz.jooble.org/api/{_apiKey}", request);

        response.EnsureSuccessStatusCode();

        var joobleResponse =
            await response.Content.ReadFromJsonAsync<JoobleSearchResponse>();

        if (joobleResponse is null)
        {
            return [];
        }

        return joobleResponse.Jobs
            .Select(job => new ExternalJob
            {
                ExternalId = job.Id.ToString(),
                Company = job.Company,
                Position = job.Title,
                Location = job.Location,
                Url = job.Link,
                Description = job.Snippet
            })
            .ToList();
    }
}