using Foodeez.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Foodeez.Integration.Tests;

/// <summary>
/// Base WebApplicationFactory for integration tests.
/// Replaces the MySQL DbContext with an in-memory SQLite database.
/// </summary>
public class FoodeezWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real MySQL DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory SQLite for integration tests
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"FoodeezIntegrationTest-{Guid.NewGuid()}"));
        });
    }
}
