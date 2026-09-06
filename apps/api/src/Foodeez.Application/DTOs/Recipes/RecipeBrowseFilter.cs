namespace Foodeez.Application.DTOs.Recipes;

/// <summary>
/// What a browse of the recipe library is asking for.
///
/// This exists because the filtering used to happen in the clients, over whatever pages the
/// infinite scroll had already fetched. A pill therefore filtered the part of the library you
/// had scrolled past rather than the library - which reads as the filter being broken, since a
/// recipe you know you have simply is not there until you scroll far enough to load it.
/// </summary>
public sealed record RecipeBrowseFilter
{
    /// <summary>Tags a recipe must carry all of. Matched as a substring, case-insensitively.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Who is asking, or null when nobody is signed in.
    ///
    /// Load-bearing beyond the two filters below: an AI-generated recipe belongs to the person
    /// it was generated for, and is not shown to anyone else at all.
    /// </summary>
    public Guid? ViewerId { get; init; }

    /// <summary>Only the meals the assistant wrote for this viewer.</summary>
    public bool OnlyPreviousMeals { get; init; }

    /// <summary>Only the recipes this viewer has marked.</summary>
    public bool OnlyFavorites { get; init; }
}
