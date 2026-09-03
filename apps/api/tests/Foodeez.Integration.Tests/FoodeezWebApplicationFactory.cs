using Foodeez.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Foodeez.Integration.Tests;

/// <summary>
/// Base WebApplicationFactory for integration tests.
/// Replaces the MySQL DbContext with an in-memory database.
/// </summary>
public class FoodeezWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real MySQL registration. Taking out DbContextOptions<AppDbContext>
            // alone is not enough: AddDbContext also registers provider-specific services, and
            // leaving those behind puts two providers in one container, which EF rejects with
            // "Only a single database provider can be registered in a service provider."
            var doomed = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(AppDbContext)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in doomed)
                services.Remove(descriptor);

            // Add the in-memory provider for integration tests
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"FoodeezIntegrationTest-{Guid.NewGuid()}"));
        });
    }
}
