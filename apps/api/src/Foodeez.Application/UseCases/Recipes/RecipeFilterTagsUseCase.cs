using System.Text.Json;
using Foodeez.Application.Common;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// The filter pills every client shows above its recipe results.
///
/// They are a curated shortlist rather than every tag present in the data, so the list lives
/// in the settings table where an administrator can edit it, and each client reads it on load
/// instead of carrying its own hardcoded copy.
/// </summary>
public class RecipeFilterTagsUseCase
{
    /// <summary>The settings row the admin panel writes: a JSON array of strings.</summary>
    public const string SettingKey = "recipes.filterTags";

    /// <summary>
    /// What the clients showed before the setting existed, and what they get while no row has
    /// been saved. Must match DEFAULT_RECIPE_FILTER_TAGS in @foodeez/shared.
    /// </summary>
    public static readonly IReadOnlyList<string> Defaults =
        ["Vegetarian", "Vegan", "High-Protein", "Low-Carb", "Quick", "Gluten-Free", "Dairy-Free"];

    private readonly IUnitOfWork _unitOfWork;

    public RecipeFilterTagsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<string>> ExecuteAsync() =>
        Parse(await _unitOfWork.AppSettings.GetValueAsync(SettingKey));

    /// <summary>
    /// A missing or blank row means "never configured", which is the defaults - deliberately
    /// not the same as an empty array, which is an administrator switching the pills off.
    /// Anything that is not JSON is read as a comma-separated list, so a row edited by hand
    /// still works.
    /// </summary>
    public static IReadOnlyList<string> Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return Defaults;

        var raw = stored.TrimStart().StartsWith('[')
            ? ReadJson(stored)
            : stored.Split(',');

        return raw is null ? Defaults : Clean(raw);
    }

    private static string?[]? ReadJson(string stored)
    {
        try
        {
            return JsonSerializer.Deserialize<string?[]>(stored);
        }
        catch (JsonException)
        {
            // Malformed JSON is a corrupt row, not an instruction to show nothing.
            return null;
        }
    }

    /// <summary>
    /// Trims, drops blanks, and keeps the first spelling of a tag repeated in another case -
    /// the pills are compared against recipe tags case-insensitively, so "Vegan" and "vegan"
    /// would be two pills that filter identically.
    /// </summary>
    private static IReadOnlyList<string> Clean(IEnumerable<string?> tags) =>
        tags.Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag!.Trim())
            .DistinctBy(tag => tag.ToLowerInvariant())
            .ToList();
}
