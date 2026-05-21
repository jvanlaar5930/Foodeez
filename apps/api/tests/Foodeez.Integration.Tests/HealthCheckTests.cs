using FluentAssertions;
using Xunit;

namespace Foodeez.Integration.Tests;

/// <summary>
/// Integration tests using WebApplicationFactory.
/// These tests spin up the full API in memory and exercise HTTP endpoints.
/// Note: A running MySQL instance is NOT required — the factory swaps in an in-memory database.
/// </summary>
public class HealthCheckTests : IClassFixture<FoodeezWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(FoodeezWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
