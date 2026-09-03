using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Foodeez.Infrastructure.Data;

/// <summary>
/// Used only by the `dotnet ef` tooling, which has to construct a context without starting the
/// app. It still needs a real connection string, so it reads the same one the app uses rather
/// than carrying a hardcoded copy - a literal here is a committed password, and it silently
/// goes stale the moment anyone's local database differs from whoever wrote it.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string EnvironmentVariable = "ConnectionStrings__Default";

    public AppDbContext CreateDbContext(string[] args)
    {
        // The API project's user-secrets are the usual source in development; the environment
        // variable covers CI and anyone running the tooling from elsewhere.
        var connectionString = Environment.GetEnvironmentVariable(EnvironmentVariable)
            ?? UserSecretsConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No connection string for the EF tooling. Either set it for the API project:\n" +
                "  dotnet user-secrets set \"ConnectionStrings:Default\" \"<connection string>\" " +
                "--project apps/api/src/Foodeez.API\n" +
                $"or set the {EnvironmentVariable} environment variable for this command.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string? UserSecretsConnectionString()
    {
        // UserSecretsId of Foodeez.API. Referenced by value because Infrastructure does not
        // depend on the API project; if that id ever changes, change it here too.
        const string userSecretsId = "04be86dd-4300-40b8-83a4-4a7fdcbf8080";

        return new ConfigurationBuilder()
            .AddUserSecrets(userSecretsId)
            .Build()["ConnectionStrings:Default"];
    }
}
