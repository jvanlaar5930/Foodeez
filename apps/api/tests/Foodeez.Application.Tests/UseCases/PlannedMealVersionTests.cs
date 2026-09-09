using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.MealPlans;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// Which version of a recipe a calendar slot holds.
///
/// An enhanced recipe carries the name of the one it elevates, deliberately - it is the same
/// dish. That leaves the calendar unable to say anything about which of the two is being
/// cooked on Thursday, since both render as "Roast Chicken", so the entry carries the answer
/// rather than the clients guessing it from a name.
/// </summary>
public class PlannedMealVersionTests
{
    private static readonly DateOnly Monday = new(2026, 1, 5);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMealPlanRepository> _plans = new();
    private readonly Mock<IRecipeRepository> _recipes = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly MealPlan _plan;

    public PlannedMealVersionTests()
    {
        _unitOfWork.Setup(u => u.MealPlans).Returns(_plans.Object);
        _unitOfWork.Setup(u => u.Recipes).Returns(_recipes.Object);

        _plan = new MealPlan
        {
            UserId = _userId,
            StartDate = Monday,
            EndDate = Monday.AddDays(6)
        };

        _plans.Setup(r => r.GetWithEntriesAsync(It.IsAny<Guid>())).ReturnsAsync(_plan);

        // What the database does for real: a saved entry is part of the plan the next read
        // loads. Without it an edit cannot find the slot that was just added.
        _plans.Setup(r => r.AddEntryAsync(It.IsAny<MealPlanEntry>()))
            .Callback<MealPlanEntry>(entry => _plan.Entries.Add(entry));
    }

    private SaveMealPlanEntryUseCase Build() => new(_unitOfWork.Object);

    /// <summary>A recipe the save path can resolve, standing in for a row in the library.</summary>
    private Recipe Available(string name, Guid? enhancedFrom = null)
    {
        var recipe = new Recipe
        {
            Name = name,
            Instructions = "Cook it.",
            Servings = 2,
            EnhancedFromRecipeId = enhancedFrom,
            IsAIGenerated = enhancedFrom != null,
        };

        _recipes.Setup(r => r.GetByIdAsync(recipe.Id)).ReturnsAsync(recipe);
        return recipe;
    }

    private MealPlanEntryRequest Plan(Recipe recipe) => new()
    {
        EntryDate = Monday,
        MealType = MealType.Dinner,
        RecipeId = recipe.Id,
        Servings = 1f,
    };

    [Fact]
    public async Task PlanningTheEnhancedVersion_SaysSoOnTheEntry()
    {
        var original = Available("Roast Chicken");
        var enhanced = Available("Roast Chicken", enhancedFrom: original.Id);

        var result = await Build().AddAsync(_plan.Id, _userId, Plan(enhanced));

        result.Outcome.Should().Be(SaveEntryOutcome.Saved);
        result.Entry!.RecipeName.Should().Be("Roast Chicken");
        result.Entry.RecipeIsEnhanced.Should().BeTrue();
    }

    [Fact]
    public async Task PlanningTheOriginal_SaysNothingOfTheSort()
    {
        var original = Available("Roast Chicken");

        var result = await Build().AddAsync(_plan.Id, _userId, Plan(original));

        result.Entry!.RecipeIsEnhanced.Should().BeFalse();
    }

    /// <summary>
    /// A meal the assistant planned is AI-written but is nobody's enhanced version, and must
    /// not be badged as one - the star means "the elevated take on a recipe you have", not
    /// "written by a model".
    /// </summary>
    [Fact]
    public async Task AGeneratedMeal_IsNotAnEnhancedVersion()
    {
        var generated = new Recipe
        {
            Name = "Generated Curry",
            Instructions = "Cook it.",
            Servings = 1,
            IsAIGenerated = true,
            CreatedByUserId = _userId,
        };
        _recipes.Setup(r => r.GetByIdAsync(generated.Id)).ReturnsAsync(generated);

        var result = await Build().AddAsync(_plan.Id, _userId, Plan(generated));

        result.Entry!.RecipeIsEnhanced.Should().BeFalse();
    }

    /// <summary>
    /// Editing a slot to point at the enhanced version has to update the flag too, or the
    /// calendar keeps showing the answer for the recipe that used to be there.
    /// </summary>
    [Fact]
    public async Task SwitchingASlotToTheEnhancedVersion_UpdatesWhatTheCalendarShows()
    {
        var original = Available("Roast Chicken");
        var enhanced = Available("Roast Chicken", enhancedFrom: original.Id);

        var added = await Build().AddAsync(_plan.Id, _userId, Plan(original));
        var switched = await Build().UpdateAsync(_plan.Id, added.Entry!.Id, _userId, Plan(enhanced));

        switched.Outcome.Should().Be(SaveEntryOutcome.Saved);
        switched.Entry!.RecipeIsEnhanced.Should().BeTrue();
    }
}
