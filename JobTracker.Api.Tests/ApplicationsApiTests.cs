using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Api.Tests;

public class ApplicationsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

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
            request
        );

        response.EnsureSuccessStatusCode();

        var application =
            await response.Content
                .ReadFromJsonAsync<JobApplicationResponse>();

        return application
            ?? throw new InvalidOperationException(
                "API returned no application."
            );
    }

    [Fact]
    public async Task GetUnknownApplication_Returns404()
    {
        // Act
        var response =
            await _client.GetAsync("/api/applications/999999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateValidApplication_Returns201()
    {
        // Arrange
        var request = new CreateJobApplicationRequest
        {
            Company = "NXP",
            Position = "Software Engineering Intern",
            Status = ApplicationStatus.Applied,
            Location = "Brno",
            JobUrl = "https://example.com/nxp-job"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            request
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

        var application =
            await response.Content
                .ReadFromJsonAsync<JobApplicationResponse>();

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
        // Arrange
        var request = new CreateJobApplicationRequest
        {
            Company = "",
            Position = "",
            Status = ApplicationStatus.Applied,
            JobUrl = "this-is-not-a-url"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            request
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task GetExistingApplication_Returns200()
    {
        // Arrange
        var created = await CreateApplication();

        // Act
        var response = await _client.GetAsync(
            $"/api/applications/{created.Id}"
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var application =
            await response.Content
                .ReadFromJsonAsync<JobApplicationResponse>();

        Assert.NotNull(application);
        Assert.Equal(created.Id, application.Id);
        Assert.Equal("Red Hat", application.Company);
        Assert.Equal("Backend Intern", application.Position);
    }

    [Fact]
    public async Task UpdateExistingApplication_Returns204()
    {
        // Arrange
        var created = await CreateApplication();

        var update = new UpdateJobApplicationRequest
        {
            Company = "Red Hat",
            Position = "Backend Software Engineer Intern",
            Status = ApplicationStatus.Interview,
            Location = "Brno",
            JobUrl = "https://example.com/job"
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/applications/{created.Id}",
            update
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode
        );

        // Also verify the changes really persisted
        var getResponse = await _client.GetAsync(
            $"/api/applications/{created.Id}"
        );

        var application =
            await getResponse.Content
                .ReadFromJsonAsync<JobApplicationResponse>();

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
        // Arrange
        var created = await CreateApplication();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/applications/{created.Id}"
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode
        );

        // Make sure it's actually gone
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
        // Arrange
        await CreateApplication(
            company: "Red Hat",
            status: ApplicationStatus.Interview
        );

        await CreateApplication(
            company: "NXP",
            status: ApplicationStatus.Applied
        );

        // Act
        var response = await _client.GetAsync(
            "/api/applications?status=Interview"
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var applications =
            await response.Content
                .ReadFromJsonAsync<List<JobApplicationResponse>>();

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