using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Infrastructure.Services.Json;
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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Spoonacular nulls out numeric fields it has no value for. Without these a single
        // "preparationMinutes": null fails the entire response and the search returns nothing.
        Converters =
        {
            new NullTolerantInt32Converter(),
            new NullTolerantDoubleConverter(),
            new NullTolerantBooleanConverter()
        }
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

    public async Task<SpoonacularSearchPage> SearchRecipesPagedAsync(string query, int offset, int number, CancellationToken ct = default)
    {
        var url = $"recipes/complexSearch?query={Uri.EscapeDataString(query)}&number={number}&offset={offset}" +
                  $"&addRecipeInformation=true&addRecipeNutrition=true&apiKey={_apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<SpoonacularSearchResponse>(json, JsonOptions);
            return new SpoonacularSearchPage
            {
                Results = result?.Results ?? [],
                TotalResults = result?.TotalResults ?? 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spoonacular paged search failed for query '{Query}' at offset {Offset}", query, offset);
            return new SpoonacularSearchPage();
        }
    }

    public async Task<IReadOnlyList<SpoonacularAutocompleteResult>> AutocompleteAsync(string query, int number = 8, CancellationToken ct = default)
    {
        var url = $"recipes/autocomplete?query={Uri.EscapeDataString(query)}&number={number}&apiKey={_apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<List<SpoonacularAutocompleteResult>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spoonacular autocomplete failed for query '{Query}'", query);
            return [];
        }
    }

    public async Task<SpoonacularRecipeResult?> GetRecipeInformationAsync(int spoonacularId, CancellationToken ct = default)
    {
        var url = $"recipes/{spoonacularId}/information?includeNutrition=true&apiKey={_apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<SpoonacularRecipeResult>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spoonacular detail lookup failed for recipe {RecipeId}", spoonacularId);
            return null;
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
