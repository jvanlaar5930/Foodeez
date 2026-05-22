namespace Foodeez.Application.Interfaces.Services;

public interface IFoodDataService
{
    Task<IReadOnlyList<FdcFoodResult>> SearchFoodsAsync(string query, int pageSize = 20, CancellationToken ct = default);
}

public class FdcFoodResult
{
    public int FdcId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? BrandOwner { get; set; }
    public string? BrandName { get; set; }
    public float ServingSize { get; set; }
    public string ServingSizeUnit { get; set; } = string.Empty;
    public string? FoodCategory { get; set; }
    public string? GtinUpc { get; set; }
    public string? DataType { get; set; }
    public List<FdcNutrient> FoodNutrients { get; set; } = new();
}

public class FdcNutrient
{
    public int NutrientId { get; set; }
    public string NutrientName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public double Value { get; set; }
}
