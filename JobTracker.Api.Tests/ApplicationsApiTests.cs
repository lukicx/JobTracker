using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using JobTracker.DAL.Data;
using JobTracker.BL.Dtos;
using JobTracker.DAL.Models;
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

        var registerResponse = await client.PostAsJsonAsync("/api/register" , request);
        registerResponse.EnsureSuccessStatusCode();
        var loginResponse = await client.PostAsJsonAsync("/api/login" , request);
        loginResponse.EnsureSuccessStatusCode();

        var loginData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        string? accessToken  = loginData.GetProperty("accessToken").GetString();

        if (accessToken is null)
        {
            throw new InvalidOperationException("Login response did not contain an accessToken.");
        }
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        await AuthenticateClient(clientA, "owner@example.com");
        await AuthenticateClient(clientB, "other@example.com");

        
        var createRequest  = new
        {
            Company = "Example Company",
            Position = "Intern",
            Status = ApplicationStatus.Interested,
            Location = "Brno",
            JobUrl = "https://example.com"
        };
        var createResponse = await clientA.PostAsJsonAsync("/api/applications", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdApplication = await createResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(createdApplication);
        var applicationUrl = $"/api/applications/{createdApplication.Id}";

        var ownerListResponse = await clientA.GetAsync("/api/applications");
        Assert.Equal(HttpStatusCode.OK, ownerListResponse.StatusCode);
        var ownerApplications = await ownerListResponse.Content.ReadFromJsonAsync<List<JobApplicationResponse>>(JsonOptions);
        Assert.NotNull(ownerApplications);
        Assert.Equal(createdApplication.Id, Assert.Single(ownerApplications).Id);

        var otherListResponse = await clientB.GetAsync("/api/applications");
        Assert.Equal(HttpStatusCode.OK, otherListResponse.StatusCode);
        var otherApplications = await otherListResponse.Content.ReadFromJsonAsync<List<JobApplicationResponse>>(JsonOptions);
        Assert.NotNull(otherApplications);
        Assert.Empty(otherApplications);

        var otherGetResponse = await clientB.GetAsync(applicationUrl);
        Assert.Equal(HttpStatusCode.NotFound, otherGetResponse.StatusCode);

        var otherUpdateResponse = await clientB.PutAsJsonAsync(applicationUrl, new
        {
            Company = "Changed Company",
            Position = "Changed Position",
            Status = ApplicationStatus.Offer,
            Location = "Changed Location",
            JobUrl = "https://example.com/changed"
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, otherUpdateResponse.StatusCode);

        var otherStatusResponse = await clientB.PatchAsJsonAsync($"{applicationUrl}/status", new
        {
            Status = ApplicationStatus.Rejected
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, otherStatusResponse.StatusCode);

        var otherDeleteResponse = await clientB.DeleteAsync(applicationUrl);
        Assert.Equal(HttpStatusCode.NotFound, otherDeleteResponse.StatusCode);

        var ownerGetResponse = await clientA.GetAsync(applicationUrl);
        Assert.Equal(HttpStatusCode.OK, ownerGetResponse.StatusCode);
        var storedApplication = await ownerGetResponse.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(storedApplication);
        Assert.Equal(createRequest.Company, storedApplication.Company);
        Assert.Equal(createRequest.Position, storedApplication.Position);
        Assert.Equal(createRequest.Status, storedApplication.Status);
        Assert.Equal(createRequest.Location, storedApplication.Location);
        Assert.Equal(createRequest.JobUrl, storedApplication.JobUrl);

        var ownerDeleteResponse = await clientA.DeleteAsync(applicationUrl);
        Assert.Equal(HttpStatusCode.NoContent, ownerDeleteResponse.StatusCode);
    }
}
