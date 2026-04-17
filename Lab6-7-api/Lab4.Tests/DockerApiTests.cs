using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lab4.Tests;

public class DockerApiTests : IAsyncLifetime
{
    private INetwork _network = null!;
    private PostgreSqlContainer _postgres = null!;
    private IContainer _apiContainer = null!;
    private HttpClient _httpClient = null!;

    public async ValueTask InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName("test-network-" + Guid.NewGuid().ToString("D"))
            .Build();

        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .WithDatabase("lab4db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        var connectionString = "Host=db;Port=5432;Database=lab4db;Username=postgres;Password=postgres;";

        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(Path.Combine(AppContext.BaseDirectory, "../../../../"))
            .WithDockerfile("Lab6.Api/Dockerfile")
            .Build();

        await image.CreateAsync();

        _apiContainer = new ContainerBuilder()
            .WithImage(image)
            .WithNetwork(_network)
            .WithPortBinding(8080, true)
            .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
            .WithEnvironment("DB_CONNECTION_STRING", connectionString)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort(8080)
                    .ForPath("/health/ready")))
            .Build();

        await _apiContainer.StartAsync();

        var apiPort = _apiContainer.GetMappedPublicPort(8080);
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{apiPort}")
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _apiContainer.DisposeAsync();
        await _postgres.DisposeAsync();
        await _network.DisposeAsync();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task CanCreateAndGetStudent_ViaDockerApi()
    {
        // Arrange
        var request = new { FullName = "Docker Student", Email = "docker@test.com", EnrollmentDate = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var createResponse = await _httpClient.PostAsync("/api/Student", content);

        // Assert
        createResponse.EnsureSuccessStatusCode(); // 201 Created
        var createdJson = await createResponse.Content.ReadAsStringAsync();
        createdJson.ShouldContain("Docker Student");
    }

    [Fact]
    public async Task UniqueConstraints_EnforcedInRealDb_ComparingToInMemory()
    {
        // Arrange
        var request = new { FullName = "Dup Student", Email = "dup@test.com", EnrollmentDate = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(request);
        var content1 = new StringContent(json, Encoding.UTF8, "application/json");
        var content2 = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        await _httpClient.PostAsync("/api/Student", content1); // Should succeed
        var response2 = await _httpClient.PostAsync("/api/Student", content2); // Should trigger duplicate key error

        // Assert
        response2.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task Migrations_EnsureCreated_RunsCleanlyOnStartup()
    {
        var request = new { FullName = "Migrate Student", Email = "migrate@test.com", EnrollmentDate = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/Student", content);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Update_ApiEndpoint_ReturnsSuccess()
    {
        // First POST to create
        var request = new { FullName = "Update Me", Email = "update@test.com", EnrollmentDate = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var createResponse = await _httpClient.PostAsync("/api/Student", content);
        createResponse.EnsureSuccessStatusCode();

        // Read created student ID from response body
        var createdJson = await createResponse.Content.ReadAsStringAsync();
        var studentDoc = JsonDocument.Parse(createdJson);
        var studentId = studentDoc.RootElement.GetProperty("id").GetInt32();

        // PUT request to update
        var updateReq = new { Id = studentId, FullName = "Updated Student", Email = "update@test.com" };
        var updateJson = JsonSerializer.Serialize(updateReq);
        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        var updateResponse = await _httpClient.PutAsync("/api/Student", updateContent);
        updateResponse.EnsureSuccessStatusCode();
        
        var updatedResponseText = await updateResponse.Content.ReadAsStringAsync();
        updatedResponseText.ShouldContain("Updated Student");
    }
}
