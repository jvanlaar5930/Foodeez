using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

public class UsdaFoodDataService : IFoodDataService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<UsdaFoodDataService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public UsdaFoodDataService(HttpClient httpClient, IConfiguration configuration, ILogger<UsdaFoodDataService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/");
        _apiKey = configuration["FoodData:ApiKey"] ?? throw new InvalidOperationException("FoodData:ApiKey configuration is required.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<FdcFoodResult>> SearchFoodsAsync(string query, int pageSize = 20, CancellationToken ct = default)
    {
        var url = $"foods/search?query={Uri.EscapeDataString(query)}&pageSize={pageSize}&api_key={_apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<FdcSearchResponse>(json, JsonOptions);

            return result?.Foods.Select(MapToResult).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "USDA FDC search failed for query '{Query}'", query);
            return [];
        }
    }

    private static FdcFoodResult MapToResult(FdcFoodItem item) => new FdcFoodResult
    {
        FdcId = item.FdcId,
        Description = item.Description,
        BrandOwner = item.BrandOwner,
        BrandName = item.BrandName,
        ServingSize = item.ServingSize,
        ServingSizeUnit = item.ServingSizeUnit ?? "g",
        FoodCategory = item.FoodCategory,
        GtinUpc = item.GtinUpc,
        DataType = item.DataType,
        FoodNutrients = item.FoodNutrients.Select(n => new FdcNutrient
        {
            NutrientId = n.NutrientId,
            NutrientName = n.NutrientName,
            UnitName = n.UnitName,
            Value = n.Value,
        }).ToList(),
    };

    // ── Internal deserialization types ────────────────────────────────────────

    private class FdcSearchResponse
    {
        public List<FdcFoodItem> Foods { get; set; } = new();
        public int TotalHits { get; set; }
    }

    private class FdcFoodItem
    {
        public int FdcId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? BrandOwner { get; set; }
        public string? BrandName { get; set; }
        public float ServingSize { get; set; }
        public string? ServingSizeUnit { get; set; }
        public string? FoodCategory { get; set; }
        public string? GtinUpc { get; set; }
        public string? DataType { get; set; }
        public List<FdcFoodNutrient> FoodNutrients { get; set; } = new();
    }

    private class FdcFoodNutrient
    {
        public int NutrientId { get; set; }
        public string NutrientName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
