namespace Foodeez.API.Configuration;

/// <summary>
/// Checks, at startup, that the configuration the API cannot run without is actually present.
///
/// The point is where the failure lands. Config is supplied differently per environment -
/// user-secrets locally, environment variables when deployed - so the way it goes missing is
/// by an environment being set up wrong, and that should stop the process with the key's name
/// in the message. The alternative is what this replaces: a blank key sits unnoticed until
/// someone's first login or first AI call fails with something that reads like a bug.
///
/// Only genuinely required values are checked. Provider API keys are not: which provider is
/// active is chosen at runtime from the database, so demanding all four would refuse to boot
/// an installation that only ever uses one of them.
/// </summary>
internal static class RequiredConfiguration
{
    /// <summary>HS256 signing needs a 256-bit key; a shorter one throws deep inside JWT setup.</summary>
    private const int MinimumJwtKeyLength = 32;

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var missing = new List<string>();

        foreach (var key in new[] { "ConnectionStrings:Default", "Jwt:Key", "Jwt:Issuer", "Jwt:Audience" })
        {
            if (string.IsNullOrWhiteSpace(configuration[key]))
                missing.Add(key);
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(BuildMessage(missing, environment));

        var jwtKey = configuration["Jwt:Key"]!;
        if (jwtKey.Length < MinimumJwtKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {MinimumJwtKeyLength} characters; it is {jwtKey.Length}. " +
                "Generate a new one and set it the same way as the other secrets.");
        }
    }

    private static string BuildMessage(IReadOnlyCollection<string> missing, IHostEnvironment environment)
    {
        var how = environment.IsDevelopment()
            ? "Set them with: dotnet user-secrets set \"<Key>\" \"<value>\" " +
              "--project apps/api/src/Foodeez.API (see the README's first-time setup)."
            : "Supply them as environment variables, replacing ':' with '__' - " +
              "Jwt:Key becomes Jwt__Key. docker-compose reads these from .env; see .env.example.";

        return $"Required configuration is missing in the '{environment.EnvironmentName}' environment: " +
               $"{string.Join(", ", missing)}. {how}";
    }
}
