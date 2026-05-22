using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

public class SpoonacularService : ISpoonacularService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<SpoonacularService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SpoonacularService(HttpClient httpClient, IConfiguration configuration, ILogger<SpoonacularService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.spoonacular.com/");
        _apiKey = configuration["Spoonacular:ApiKey"] ?? throw new InvalidOperationException("Spoonacular:ApiKey configuration is required.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<SpoonacularRecipeResult>> SearchRecipesAsync(string query, int number = 10, CancellationToken ct = default)
    {
        var url = $"recipes/complexSearch?query={Uri.EscapeDataString(query)}&number={number}&addRecipeInformation=true&addRecipeNutrition=true&apiKey={_apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<SpoonacularSearchResponse>(json, JsonOptions);
            return result?.Results ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spoonacular search failed for query '{Query}'", query);
            return [];
        }
    }

    private class SpoonacularSearchResponse
    {
        public List<SpoonacularRecipeResult> Results { get; set; } = new();
        public int Offset { get; set; }
        public int Number { get; set; }
        public int TotalResults { get; set; }
    }
}
