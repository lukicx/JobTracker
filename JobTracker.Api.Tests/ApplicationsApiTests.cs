using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Api.Tests;

public class ApplicationsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
    
    public ApplicationsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        ResetDatabase();
    }

    private void ResetDatabase()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    private async Task<JobApplicationResponse> CreateApplication(
        string company = "Red Hat",
        string position = "Backend Intern",
        ApplicationStatus status = ApplicationStatus.Applied)
    {
        var request = new CreateJobApplicationRequest
        {
            Company = company,
            Position = position,
            Status = status,
            Location = "Brno",
            JobUrl = "https://example.com/job"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            request,
            JsonOptions
        );

        response.EnsureSuccessStatusCode();

        var application =
            await response.Content
                .ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);

        return application
            ?? throw new InvalidOperationException(
                "API returned no application."
            );
    }

    [Fact]
    public async Task GetUnknownApplication_Returns404()
    {
        var response =
            await _client.GetAsync("/api/applications/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateValidApplication_Returns201()
    {
        var request = new CreateJobApplicationRequest
        {
            Company = "NXP",
            Position = "Software Engineering Intern",
            Status = ApplicationStatus.Applied,
            Location = "Brno",
            JobUrl = "https://example.com/nxp-job"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            request,
            JsonOptions
        );

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

        var application =
            await response.Content
                .ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);

        Assert.NotNull(application);
        Assert.True(application.Id > 0);
        Assert.Equal("NXP", application.Company);
        Assert.Equal(
            ApplicationStatus.Applied,
            application.Status
        );

        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateInvalidApplication_Returns400()
    {
        var request = new CreateJobApplicationRequest
        {
            Company = "",
            Position = "",
            Status = ApplicationStatus.Applied,
            JobUrl = "this-is-not-a-url"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            request,
            JsonOptions
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task GetExistingApplication_Returns200()
    {
        var created = await CreateApplication();

        var response = await _client.GetAsync(
            $"/api/applications/{created.Id}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var application =
            await response.Content
                .ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);

        Assert.NotNull(application);
        Assert.Equal(created.Id, application.Id);
        Assert.Equal("Red Hat", application.Company);
        Assert.Equal("Backend Intern", application.Position);
    }

    [Fact]
    public async Task UpdateExistingApplication_Returns204()
    {
        var created = await CreateApplication();

        var update = new UpdateJobApplicationRequest
        {
            Company = "Red Hat",
            Position = "Backend Software Engineer Intern",
            Status = ApplicationStatus.Interview,
            Location = "Brno",
            JobUrl = "https://example.com/job"
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/applications/{created.Id}",
            update,
            JsonOptions
        );

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode
        );

        var getResponse = await _client.GetAsync(
            $"/api/applications/{created.Id}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode
        );

        var application =
            await getResponse.Content
                .ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);

        Assert.NotNull(application);

        Assert.Equal(
            "Backend Software Engineer Intern",
            application.Position
        );

        Assert.Equal(
            ApplicationStatus.Interview,
            application.Status
        );
    }

    [Fact]
    public async Task DeleteExistingApplication_Returns204()
    {
        var created = await CreateApplication();

        var response = await _client.DeleteAsync(
            $"/api/applications/{created.Id}"
        );

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode
        );

        var getResponse = await _client.GetAsync(
            $"/api/applications/{created.Id}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode
        );
    }

    [Fact]
    public async Task FilterByStatus_ReturnsCorrectApplications()
    {
        await CreateApplication(
            company: "Red Hat",
            status: ApplicationStatus.Interview
        );

        await CreateApplication(
            company: "NXP",
            status: ApplicationStatus.Applied
        );

        var response = await _client.GetAsync(
            "/api/applications?status=Interview"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var applications =
            await response.Content
                .ReadFromJsonAsync<List<JobApplicationResponse>>(JsonOptions);

        Assert.NotNull(applications);

        Assert.Single(applications);

        Assert.Equal(
            ApplicationStatus.Interview,
            applications[0].Status
        );

        Assert.Equal(
            "Red Hat",
            applications[0].Company
        );
    }
}