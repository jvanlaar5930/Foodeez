using FluentAssertions;
using Foodeez.Application.UseCases.Recipes;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// One settings row drives the filter pills in both clients, and it is edited by hand often
/// enough - through the admin panel, or straight in the table - that how it is read matters
/// more than how it is written.
/// </summary>
public class RecipeFilterTagsUseCaseTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoRowMeansTheBuiltInList(string? stored)
    {
        RecipeFilterTagsUseCase.Parse(stored).Should().Equal(RecipeFilterTagsUseCase.Defaults);
    }

    [Fact]
    public void AnEmptyArrayIsAnAdministratorTurningThePillsOff_NotAnAbsentSetting()
    {
        RecipeFilterTagsUseCase.Parse("[]").Should().BeEmpty();
    }

    [Fact]
    public void TheStoredOrderIsKept()
    {
        RecipeFilterTagsUseCase.Parse("""["Keto","Vegan","Under 500 kcal"]""")
            .Should().Equal("Keto", "Vegan", "Under 500 kcal");
    }

    [Fact]
    public void BlankAndDuplicateEntriesAreDropped()
    {
        // Pills are compared to recipe tags case-insensitively, so "vegan" would be a second
        // pill that filters identically to the first.
        RecipeFilterTagsUseCase.Parse("""[" Keto ","","vegan","Vegan","   "]""")
            .Should().Equal("Keto", "vegan");
    }

    [Fact]
    public void ARowEditedByHandAsACommaSeparatedListStillWorks()
    {
        RecipeFilterTagsUseCase.Parse("Keto, Vegan ,Quick")
            .Should().Equal("Keto", "Vegan", "Quick");
    }

    [Fact]
    public void MalformedJsonFallsBackToTheDefaults_RatherThanToNoPillsAtAll()
    {
        RecipeFilterTagsUseCase.Parse("""["Keto", """)
            .Should().Equal(RecipeFilterTagsUseCase.Defaults);
    }
}
