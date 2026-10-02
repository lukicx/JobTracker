using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.BearerToken;
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
        _client = factory.CreateClient ();
    }
    
    private void ResetDatabase()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
    
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    static ApplicationsApiTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
    
    private async Task AuthenticateClient(HttpClient client, string email = "test@example.com", string password = "Test123.")
    {
        var request = new
        {
            email = email,
            password = password
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/register" , request);
        registerResponse.EnsureSuccessStatusCode();
        var loginResponse = await _client.PostAsJsonAsync("/api/login" , request);
        loginResponse.EnsureSuccessStatusCode();

        var loginData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        string? accessToken  = loginData.GetProperty("accessToken").GetString();

        if (accessToken is null)
        {
            throw new InvalidOperationException("Login response did not contain an accessToken.");
        }
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }


    [Fact]
    public async Task UnauthenticatedUser()
    {
        ResetDatabase();
        var response = await _client.GetAsync("/api/applications");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task GetAllApplications()
    {
        ResetDatabase();
        await AuthenticateClient(_client);
        var response = await _client.GetAsync("/api/applications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task CreateApplication()
    {
        ResetDatabase();
        await AuthenticateClient(_client);
        
        var request = new 
        {   
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        
        var response = await _client.PostAsJsonAsync("/api/applications" , request);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetApplicationById()
    {
        ResetDatabase();
        await AuthenticateClient(_client);

        var request = new
        {
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        var response = await _client.PostAsJsonAsync("/api/applications", request);
        var application = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        var applicationId = application?.Id;

        response = await _client.GetAsync($"/api/applications/{applicationId}");
        application = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        var storedApplicationId = application?.Id;
        
        Assert.Equal(applicationId, storedApplicationId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task GetUnknownApplicationById()
    {
        ResetDatabase();
        await AuthenticateClient(_client);

        var applicationId = 999999;
        var response = await _client.GetAsync($"/api/applications/{applicationId}");
     
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    
    [Fact]
    public async Task UpdateExistingApplication()
    {
        ResetDatabase();
        await AuthenticateClient(_client);
        
        var createRequest  = new
        {
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/applications", createRequest);
        
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdApplication = await createResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(createdApplication);
        var applicationId = createdApplication.Id;
        
        var updateRequest = new
        {
            Company = "Example Company",
            Position = "Backend Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/applications/{applicationId}", updateRequest);
     
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        
        var getResponse = await _client.GetAsync($"/api/applications/{applicationId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var updatedApplication = await getResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        
        Assert.NotNull(updatedApplication);
        Assert.Equal(updateRequest.Position, updatedApplication.Position);
        
    }
    [Fact]
    public async Task ChangeApplicationStatus()
    {
        ResetDatabase();
        await AuthenticateClient(_client);
        
        var createRequest  = new
        {
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/applications", createRequest);
        
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdApplication = await createResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(createdApplication);
        var applicationId = createdApplication.Id;
        
        var updateRequest = new
        {
            Status = ApplicationStatus.Offer,
        };

        var updateResponse = await _client.PatchAsJsonAsync($"/api/applications/{applicationId}/status", updateRequest, JsonOptions);
     
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        
        var getResponse = await _client.GetAsync($"/api/applications/{applicationId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var updatedApplication = await getResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        
        Assert.NotNull(updatedApplication);
        Assert.Equal(updateRequest.Status, updatedApplication.Status);
        
    }
    [Fact]
    public async Task DeleteOwnedApplication()
    {
        ResetDatabase();
        await AuthenticateClient(_client);
        
        var createRequest  = new
        {
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/applications", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdApplication = await createResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        var createdApplicationId = createdApplication?.Id;
        
        Assert.NotNull(createdApplicationId);
        var deleteResponse = await _client.DeleteAsync($"/api/applications/{createdApplicationId}");
        
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        
        var getResponse = await _client.GetAsync($"/api/applications/{createdApplicationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    [Fact]
    public async Task AccessOnlyOwnedApplications()
    {
        ResetDatabase();
        var clientA = _factory.CreateClient();
        var clientB = _factory.CreateClient();

        
        var createRequest  = new
        {
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/applications", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdApplication = await createResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        var createdApplicationId = createdApplication?.Id;
        
        Assert.NotNull(createdApplicationId);
        var deleteResponse = await _client.DeleteAsync($"/api/applications/{createdApplicationId}");
        
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        
        var getResponse = await _client.GetAsync($"/api/applications/{createdApplicationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}