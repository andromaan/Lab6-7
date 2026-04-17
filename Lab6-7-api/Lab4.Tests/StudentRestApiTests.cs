using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Lab4.Data;
using Shouldly;
using Xunit;
using System.Linq;

namespace Lab4.Tests;

public class StudentRestApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StudentRestApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateStudent_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateStudentRequest
        {
            FullName = "REST Test User",
            Email = $"rest-{Guid.NewGuid()}@example.com",
            EnrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/student", request);
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"ResponseBody: {responseBody}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, responseBody);
    }

    [Fact]
    public async Task CreateStudent_EmptyFullName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateStudentRequest
        {
            FullName = "", // Invalid
            Email = "valid@example.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/student", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("FullName is required and cannot exceed 100 characters.");
    }

    [Fact]
    public async Task UpdateStudent_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateStudentRequest
        {
            Id = 1,
            FullName = "Valid Name",
            Email = "invalid-email" // Invalid format
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/student", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("A valid Email is required.");
    }
}

