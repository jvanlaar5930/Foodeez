using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Application.UseCases.Recipes;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// An enhancement is the same dish raised by technique, kept beside the recipe it came from.
///
/// The rules that matter are about what does *not* happen: the original is never rewritten,
/// a second ask never produces a third version, and a provider that falls over never costs
/// somebody the enhancement they already had.
/// </summary>
public class EnhanceRecipeUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRecipeRepository> _recipes = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAIService> _ai = new();
    private readonly Mock<ISpoonacularService> _spoonacular = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Recipe _original;

    public EnhanceRecipeUseCaseTests()
    {
        _unitOfWork.Setup(u => u.Recipes).Returns(_recipes.Object);
        _unitOfWork.Setup(u => u.Users).Returns(_users.Object);

        var user = User.Create("cook@test.com", "hash", "Test", "Cook");
        user.Profile = new UserProfile { UserId = user.Id, ProfileCompleted = true };
        _users.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

        _original = new Recipe
        {
            Name = "Weeknight Tomato Pasta",
            Description = "Quick and cheap.",
            Instructions = "Boil pasta.\nHeat sauce.\nCombine.",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 15,
            Servings = 2,
            Tags = "dinner,quick",
            ImageUrl = "https://example.test/pasta.jpg",
            SourceUrl = "https://example.test/recipe",
            SourceName = "Example Kitchen",
            NutritionalInfoPerServing = new NutritionalInfo(500, 15, 80, 12, 6, 9, 400)
        };

        _recipes.Setup(r => r.GetByIdAsync(_original.Id)).ReturnsAsync(_original);
        _recipes.Setup(r => r.GetEnhancementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);
    }

    private EnhanceRecipeUseCase Build() =>
        new(_unitOfWork.Object,
            _ai.Object,
            new GetRecipeDetailUseCase(_unitOfWork.Object, _spoonacular.Object));

    /// <summary>A usable answer from the model, as a provider would hand one back.</summary>
    private void AiWrites(string description = "Built on a proper soffritto.")
    {
        _ai.Setup(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnhancedRecipeDto
            {
                Name = "Pomodoro, Elevated",
                Description = description,
                Instructions = "Salt the water heavily.\nBloom the garlic in cold oil.\nFinish in the pan.",
                PrepTimeMinutes = 10,
                CookTimeMinutes = 25,
                Servings = 2,
                Tags = ["italian"],
                ChefNotes = ["Finish the pasta in the sauce so the starch emulsifies it."],
                Ingredients =
                [
                    new EnhancedIngredientDto { Name = "San Marzano tomatoes", Quantity = 400, Unit = "g" }
                ],
                NutritionalInfoPerServing = new() { Calories = 620, Protein = 18 }
            });
    }

    /// <summary>The row a previous enhancement left behind.</summary>
    private Recipe ExistingEnhancement()
    {
        var enhancement = new Recipe
        {
            Name = _original.Name,
            Description = "The first take.",
            Instructions = "Do it the first way.",
            EnhancedFromRecipeId = _original.Id,
            CreatedByUserId = _userId,
            IsAIGenerated = true,
            EnhancementNotes = "Rest the sauce.",
            EnhancedAt = DateTime.UtcNow.AddDays(-1)
        };

        _recipes.Setup(r => r.GetEnhancementAsync(_original.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enhancement);

        return enhancement;
    }

    [Fact]
    public async Task Enhancing_writes_a_second_recipe_that_points_back_at_the_original()
    {
        AiWrites();
        Recipe? saved = null;
        _recipes.Setup(r => r.AddAsync(It.IsAny<Recipe>())).Callback<Recipe>(r => saved = r);

        var result = await Build().ExecuteAsync(_original.Id, _userId);

        saved.Should().NotBeNull();
        saved!.EnhancedFromRecipeId.Should().Be(_original.Id);
        saved.CreatedByUserId.Should().Be(_userId);
        result.IsEnhanced.Should().BeTrue();
        result.Instructions.Should().Contain("Salt the water heavily.");
        result.EnhancementNotes.Should().Contain("emulsifies");

        // The original is a library recipe other people are reading.
        _original.Instructions.Should().Be("Boil pasta.\nHeat sauce.\nCombine.");
        _original.EnhancedFromRecipeId.Should().BeNull();
    }

    /// <summary>
    /// The name is what tells a reader which dinner they are looking at. A model that renames
    /// the dish for its menu must not be allowed to make the enhanced version unrecognisable.
    /// </summary>
    [Fact]
    public async Task The_enhanced_version_keeps_the_original_name_and_picture()
    {
        AiWrites();

        var result = await Build().ExecuteAsync(_original.Id, _userId);

        result.Name.Should().Be("Weeknight Tomato Pasta");
        result.ImageUrl.Should().Be(_original.ImageUrl);
    }

    /// <summary>
    /// Opening a recipe you have already enhanced must not quietly spend another generation,
    /// which is what made "only two versions" worth enforcing on the server rather than in a
    /// button's disabled state.
    /// </summary>
    [Fact]
    public async Task Asking_again_returns_the_existing_enhancement_without_calling_the_model()
    {
        var existing = ExistingEnhancement();
        AiWrites();

        var result = await Build().ExecuteAsync(_original.Id, _userId);

        result.Id.Should().Be(existing.Id);
        result.Instructions.Should().Be("Do it the first way.");
        _ai.Verify(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _recipes.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
    }

    [Fact]
    public async Task Refreshing_rewrites_the_same_row_rather_than_adding_a_third_version()
    {
        var existing = ExistingEnhancement();
        AiWrites();

        var result = await Build().ExecuteAsync(_original.Id, _userId, refresh: true);

        result.Id.Should().Be(existing.Id);
        existing.Instructions.Should().Contain("Bloom the garlic");
        _recipes.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        _recipes.Verify(
            r => r.ReplaceIngredients(existing, It.IsAny<IReadOnlyList<RecipeIngredient>>()), Times.Once);
    }

    /// <summary>A refresh is a request for a different take, so the model is told what to avoid.</summary>
    [Fact]
    public async Task Refreshing_shows_the_model_the_version_being_replaced()
    {
        ExistingEnhancement();
        AiWrites();
        EnhanceRecipeRequest? sent = null;
        _ai.Setup(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EnhanceRecipeRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new EnhancedRecipeDto { Instructions = "Another way entirely." });

        await Build().ExecuteAsync(_original.Id, _userId, refresh: true);

        sent!.PreviousEnhancement.Should().Contain("Do it the first way.");
        sent.Original.Name.Should().Be(_original.Name);
    }

    /// <summary>
    /// The failure that matters: a refresh that could not be written must leave the reader
    /// with the enhancement they already had, not a blank one.
    /// </summary>
    [Fact]
    public async Task A_provider_failure_during_a_refresh_leaves_the_existing_enhancement_alone()
    {
        var existing = ExistingEnhancement();
        _ai.Setup(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnhancedRecipeDto { Succeeded = false });

        var enhance = Build();
        await Assert.ThrowsAsync<AIGenerationFailedException>(
            () => enhance.ExecuteAsync(_original.Id, _userId, refresh: true));

        existing.Instructions.Should().Be("Do it the first way.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>An answer with no method is no answer, whatever else came back with it.</summary>
    [Fact]
    public async Task A_rewrite_with_no_method_is_not_saved()
    {
        _ai.Setup(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnhancedRecipeDto { Description = "Sounds lovely.", Instructions = "  " });

        var enhance = Build();
        await Assert.ThrowsAsync<AIGenerationFailedException>(() => enhance.ExecuteAsync(_original.Id, _userId));

        _recipes.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
    }

    /// <summary>
    /// Enhancing an enhancement would build a chain, and there are only ever two versions of
    /// a recipe: the one that was written and the one a chef would cook.
    /// </summary>
    [Fact]
    public async Task An_enhanced_recipe_cannot_itself_be_enhanced()
    {
        var enhancement = new Recipe
        {
            Name = "Already elevated",
            Instructions = "Step.",
            EnhancedFromRecipeId = _original.Id,
            CreatedByUserId = _userId,
            IsAIGenerated = true
        };
        _recipes.Setup(r => r.GetByIdAsync(enhancement.Id)).ReturnsAsync(enhancement);
        AiWrites();

        var enhance = Build();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => enhance.ExecuteAsync(enhancement.Id, _userId));

        _ai.Verify(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_recipe_that_does_not_exist_is_reported_as_missing()
    {
        var missing = Guid.NewGuid();
        _recipes.Setup(r => r.GetByIdAsync(missing)).ReturnsAsync((Recipe?)null);

        var enhance = Build();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => enhance.ExecuteAsync(missing, _userId));
    }

    /// <summary>Reading is a lookup, never a generation - see the GET route this backs.</summary>
    [Fact]
    public async Task Reading_an_enhancement_that_was_never_asked_for_returns_nothing()
    {
        var result = await Build().GetAsync(_original.Id, _userId);

        result.Should().BeNull();
        _ai.Verify(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The exclusions come from the profile on every call. A model that is not told about an
    /// allergy will happily add butter to elevate a dish.
    /// </summary>
    [Fact]
    public async Task The_cooks_excluded_foods_are_sent_with_the_recipe()
    {
        var user = User.Create("allergic@test.com", "hash", "Test", "Cook");
        user.Profile = new UserProfile { UserId = user.Id, ProfileCompleted = true };
        user.Profile.ExcludedFoods.Add("dairy");
        _users.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

        AiWrites();
        EnhanceRecipeRequest? sent = null;
        _ai.Setup(a => a.EnhanceRecipeAsync(It.IsAny<EnhanceRecipeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EnhanceRecipeRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new EnhancedRecipeDto { Instructions = "Step." });

        await Build().ExecuteAsync(_original.Id, _userId);

        sent!.ExcludedFoods.Should().Contain("dairy");
    }
}
