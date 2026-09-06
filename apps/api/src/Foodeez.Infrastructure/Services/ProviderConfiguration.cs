using Foodeez.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Where an AI provider's settings come from: the admin settings table first, appsettings
/// second.
///
/// Only <see cref="LocalAIService"/> used to read the table, with a private copy of this
/// lookup; the other four went straight to <see cref="IConfiguration"/>. Admin &gt; Settings
/// has always offered claude.*, gemini.*, groq.* and ollama.* fields, so an administrator
/// could type an API key, save it, be told it was saved - and have the provider carry on
/// using whatever appsettings said, because nothing ever read the row back. One resolver
/// shared by every provider is what makes those fields mean something.
///
/// Scoped, like the repository it reads through: one per request, and the values are read
/// fresh each time so a setting changed in the admin panel takes effect on the next request
/// rather than on the next restart.
/// </summary>
public sealed class ProviderConfiguration
{
    private readonly IAppSettingRepository _settings;
    private readonly IConfiguration _configuration;

    public ProviderConfiguration(IAppSettingRepository settings, IConfiguration configuration)
    {
        _settings = settings;
        _configuration = configuration;
    }

    /// <summary>
    /// The value for one setting. A row that exists but is blank counts as unset: the admin
    /// form posts empty strings for fields nobody filled in, and treating those as real values
    /// would blank out what appsettings supplies.
    /// </summary>
    public async Task<string?> ResolveAsync(string settingKey, string configKey)
    {
        var value = await _settings.GetValueAsync(settingKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        value = _configuration[configKey];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>The same, for a value that has to be a positive whole number to be worth having.</summary>
    public async Task<int?> ResolveIntAsync(string settingKey, string configKey)
    {
        var raw = await ResolveAsync(settingKey, configKey);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : null;
    }

    /// <summary>
    /// How long one call to this provider may take before it is abandoned.
    ///
    /// Every provider has one and every provider's is editable, because "how long is too long"
    /// is a property of the deployment, not of the code: the same prompt answers in four
    /// seconds on Groq and in nearly two minutes on a model running on someone's CPU.
    /// </summary>
    public async Task<TimeSpan> ResolveTimeoutAsync(string settingKey, string configKey, int defaultSeconds) =>
        TimeSpan.FromSeconds(await ResolveIntAsync(settingKey, configKey) ?? defaultSeconds);
}
