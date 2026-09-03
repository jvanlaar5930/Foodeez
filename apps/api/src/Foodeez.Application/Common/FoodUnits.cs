namespace Foodeez.Application.Common;

/// <summary>
/// The small amount of unit sense quick add needs to decide whether a food it found in the
/// database is really the food that was described.
///
/// There is deliberately no conversion here. Converting "2 slices" into grams needs to know
/// how heavy a slice of that particular bread is, which nothing in this system knows, and a
/// guess would land in someone's calorie count looking exactly like a fact.
/// </summary>
public static class FoodUnits
{
    public static string Normalize(string? unit)
    {
        var raw = unit?.Trim().ToLowerInvariant() ?? string.Empty;

        return raw switch
        {
            "g" or "gram" or "grams" or "gm" => "g",
            "kg" or "kilogram" or "kilograms" => "kg",
            "mg" or "milligram" or "milligrams" => "mg",
            "ml" or "milliliter" or "milliliters" or "millilitre" or "millilitres" => "ml",
            "l" or "liter" or "liters" or "litre" or "litres" => "l",
            "oz" or "ounce" or "ounces" => "oz",
            "lb" or "lbs" or "pound" or "pounds" => "lb",
            "cup" or "cups" => "cup",
            "tbsp" or "tablespoon" or "tablespoons" => "tbsp",
            "tsp" or "teaspoon" or "teaspoons" => "tsp",
            "slice" or "slices" => "slice",
            "piece" or "pieces" => "piece",
            "" or "serving" or "servings" or "portion" or "portions" or "item" or "items" or "unit" or "units"
                => "serving",
            _ => raw
        };
    }

    /// <summary>
    /// Whether an amount stated in one unit can be read as an amount in the other without
    /// converting anything. Only identical units qualify, plus the vague ones - "1 serving"
    /// and "1 piece" of the same food mean the same helping to the person who ate it.
    /// </summary>
    public static bool AreInterchangeable(string? a, string? b)
    {
        var left = Normalize(a);
        var right = Normalize(b);

        if (left == right)
        {
            return true;
        }

        return IsWholeHelping(left) && IsWholeHelping(right);
    }

    private static bool IsWholeHelping(string normalized) =>
        normalized is "serving" or "piece" or "slice";
}
