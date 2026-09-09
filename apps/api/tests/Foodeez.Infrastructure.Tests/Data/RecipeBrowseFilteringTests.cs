using FluentAssertions;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Foodeez.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foodeez.Infrastructure.Tests.Data;

/// <summary>
/// Who can see which recipes, and what the filter pills actually select.
///
/// This is the query itself rather than a mock of it, because the two things worth pinning are
/// properties of the SQL: an AI-generated recipe is written into a library everyone reads, and
/// nothing but this WHERE clause keeps it out of everyone else's list; and the filtering has to
/// happen here at all, since doing it in the client is what made a pill hide recipes it should
/// have shown.
/// </summary>
public class RecipeBrowseFilteringTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly RecipeRepository _recipes;
    private readonly Guid _me = Guid.NewGuid();
    private readonly Guid _someoneElse = Guid.NewGuid();

    public RecipeBrowseFilteringTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"recipe-browse-{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _recipes = new RecipeRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private Recipe Add(string name, string? tags = null, bool ai = false, Guid? owner = null)
    {
        var recipe = new Recipe
        {
            Name = name,
            Tags = tags,
            IsAIGenerated = ai,
            CreatedByUserId = owner,
            Instructions = "Cook it.",
            Servings = 1,
        };

        _context.Recipes.Add(recipe);
        _context.SaveChanges();
        return recipe;
    }

    /// <summary>One person's elevated version of a recipe, which shares its name.</summary>
    private Recipe Enhance(Recipe original, Guid owner)
    {
        var enhancement = new Recipe
        {
            Name = original.Name,
            Instructions = "Cook it properly.",
            Servings = 1,
            IsAIGenerated = true,
            CreatedByUserId = owner,
            EnhancedFromRecipeId = original.Id,
            EnhancedAt = DateTime.UtcNow,
        };

        _context.Recipes.Add(enhancement);
        _context.SaveChanges();
        return enhancement;
    }

    private void Save(Guid userId, Recipe recipe)
    {
        _context.SavedRecipes.Add(new SavedRecipe { UserId = userId, RecipeId = recipe.Id });
        _context.SaveChanges();
    }

    private Task<IReadOnlyList<Recipe>> Browse(RecipeBrowseFilter filter) =>
        _recipes.BrowseAsync(filter, 0, 50);

    // ── Whose recipes are visible ────────────────────────────────────────────

    [Fact]
    public async Task AnAiRecipe_IsHiddenFromEveryoneButThePersonItWasWrittenFor()
    {
        Add("Library Lasagne");
        Add("My Generated Curry", ai: true, owner: _me);
        Add("Their Generated Stew", ai: true, owner: _someoneElse);

        var mine = await Browse(new RecipeBrowseFilter { ViewerId = _me });

        mine.Select(r => r.Name).Should().BeEquivalentTo("Library Lasagne", "My Generated Curry");
    }

    [Fact]
    public async Task ASignedOutVisitor_SeesNoGeneratedRecipesAtAll()
    {
        Add("Library Lasagne");
        Add("My Generated Curry", ai: true, owner: _me);

        var anonymous = await Browse(new RecipeBrowseFilter { ViewerId = null });

        anonymous.Select(r => r.Name).Should().BeEquivalentTo("Library Lasagne");
    }

    // ── Enhanced versions ────────────────────────────────────────────────────

    /// <summary>
    /// An enhancement is not a separate dish. It carries the name of the recipe it elevates,
    /// so listing it puts what looks like a duplicate beside every recipe anyone has ever
    /// enhanced - including for the person who asked for it, which is why this is hidden from
    /// its own author too. It is reached by opening the original.
    /// </summary>
    [Fact]
    public async Task AnEnhancedVersion_IsNotListedBesideTheRecipeItElevates()
    {
        var original = Add("Roast Chicken");
        Enhance(original, _me);

        var mine = await Browse(new RecipeBrowseFilter { ViewerId = _me });

        mine.Select(r => r.Name).Should().BeEquivalentTo("Roast Chicken");
    }

    [Fact]
    public async Task AnEnhancedVersion_IsNotAPreviousMeal()
    {
        var original = Add("Roast Chicken");
        Enhance(original, _me);
        Add("My Generated Curry", ai: true, owner: _me);

        var previous = await Browse(new RecipeBrowseFilter { ViewerId = _me, OnlyPreviousMeals = true });

        previous.Select(r => r.Name).Should().BeEquivalentTo("My Generated Curry");
    }

    [Fact]
    public async Task AnEnhancedVersion_IsFoundByTheOwnerWhoAskedForIt()
    {
        var original = Add("Roast Chicken");
        var mine = Enhance(original, _me);
        Enhance(original, _someoneElse);

        var found = await _recipes.GetEnhancementAsync(original.Id, _me);

        found!.Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task AnEnhancedVersion_IsNotVisibleToWhoeverDidNotAskForIt()
    {
        var original = Add("Roast Chicken");
        Enhance(original, _someoneElse);

        var found = await _recipes.GetEnhancementAsync(original.Id, _me);

        found.Should().BeNull();
    }

    /// <summary>
    /// A generated plan naming a dish you have enhanced must land on the recipe, not on your
    /// enhancement of it - they share a name, which is the only thing this query matches on.
    /// </summary>
    [Fact]
    public async Task ReusingARecipeByName_NeverPicksUpAnEnhancement()
    {
        var original = Add("Roast Chicken", ai: true, owner: _me);
        Enhance(original, _me);

        var reusable = await _recipes.GetOwnedByNamesAsync(_me, ["Roast Chicken"]);

        reusable.Should().ContainSingle().Which.Id.Should().Be(original.Id);
    }

    // ── Previous Meals ───────────────────────────────────────────────────────

    [Fact]
    public async Task PreviousMeals_ReturnsOnlyTheMealsThisUsersAssistantWrote()
    {
        Add("Library Lasagne");
        Add("My Generated Curry", ai: true, owner: _me);
        Add("Their Generated Stew", ai: true, owner: _someoneElse);

        var previous = await Browse(new RecipeBrowseFilter { ViewerId = _me, OnlyPreviousMeals = true });

        previous.Select(r => r.Name).Should().BeEquivalentTo("My Generated Curry");
    }

    [Fact]
    public async Task PreviousMeals_ReturnsNothingForASignedOutVisitor_RatherThanEverybodys()
    {
        Add("My Generated Curry", ai: true, owner: _me);
        Add("Their Generated Stew", ai: true, owner: _someoneElse);

        var previous = await Browse(new RecipeBrowseFilter { ViewerId = null, OnlyPreviousMeals = true });

        previous.Should().BeEmpty();
    }

    // ── Favorites ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Favorites_ReturnsOnlyWhatThisUserMarked()
    {
        var kept = Add("Library Lasagne");
        Add("Unkept Pie");
        var theirs = Add("Their Pick");

        Save(_me, kept);
        Save(_someoneElse, theirs);

        var favorites = await Browse(new RecipeBrowseFilter { ViewerId = _me, OnlyFavorites = true });

        favorites.Select(r => r.Name).Should().BeEquivalentTo("Library Lasagne");
    }

    [Fact]
    public async Task Favorites_ReturnsNothingForASignedOutVisitor()
    {
        var kept = Add("Library Lasagne");
        Save(_me, kept);

        var favorites = await Browse(new RecipeBrowseFilter { ViewerId = null, OnlyFavorites = true });

        favorites.Should().BeEmpty();
    }

    // ── Tags, and combining ──────────────────────────────────────────────────

    [Fact]
    public async Task Tags_NarrowToRecipesCarryingAllOfThem()
    {
        Add("Vegan Quick Bowl", tags: "vegan,quick");
        Add("Vegan Slow Stew", tags: "vegan");
        Add("Quick Steak", tags: "quick");

        var both = await Browse(new RecipeBrowseFilter { ViewerId = _me, Tags = ["vegan", "quick"] });

        both.Select(r => r.Name).Should().BeEquivalentTo("Vegan Quick Bowl");
    }

    [Fact]
    public async Task ASpecialFilterAndATag_NarrowTogether()
    {
        var keptVegan = Add("Kept Vegan Bowl", tags: "vegan");
        var keptMeat = Add("Kept Steak", tags: "beef");
        Add("Unkept Vegan Pie", tags: "vegan");

        Save(_me, keptVegan);
        Save(_me, keptMeat);

        var favoriteVegan = await Browse(new RecipeBrowseFilter
        {
            ViewerId = _me,
            OnlyFavorites = true,
            Tags = ["vegan"],
        });

        favoriteVegan.Select(r => r.Name).Should().BeEquivalentTo("Kept Vegan Bowl");
    }

    /// <summary>
    /// The point of moving this to the database: a match on page four is found without the
    /// client having scrolled through the three pages before it.
    /// </summary>
    [Fact]
    public async Task AMatchBeyondTheFirstPage_IsStillFound()
    {
        for (var i = 0; i < 30; i++)
        {
            Add($"Filler {i}", tags: "quick");
        }

        var needle = Add("The Kept One", tags: "vegan");
        Save(_me, needle);

        var favorites = await _recipes.BrowseAsync(
            new RecipeBrowseFilter { ViewerId = _me, OnlyFavorites = true }, 0, 10);

        favorites.Select(r => r.Name).Should().BeEquivalentTo("The Kept One");
    }
}
