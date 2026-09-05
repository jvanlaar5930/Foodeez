using FluentAssertions;
using Foodeez.Domain.Entities;
using Xunit;

namespace Foodeez.Domain.Tests.Entities;

/// <summary>
/// Exclusions are typed by hand and go straight into an AI prompt, so what the list may
/// contain is a rule about the profile rather than about the endpoint that happened to set
/// it - which is where this used to live.
/// </summary>
public class UserProfileExclusionTests
{
    private static UserProfile Profile() => new();

    [Fact]
    public void EntriesAreTrimmed()
    {
        var profile = Profile();

        profile.SetExcludedFoods(["  peanuts ", "shellfish"]);

        profile.ExcludedFoods.Should().Equal("peanuts", "shellfish");
    }

    [Fact]
    public void BlanksAreDropped()
    {
        var profile = Profile();

        profile.SetExcludedFoods(["peanuts", "", "   "]);

        profile.ExcludedFoods.Should().Equal("peanuts");
    }

    [Fact]
    public void DuplicatesAreRemovedIgnoringCase()
    {
        // "Peanuts", "peanuts " and "PEANUTS" are one allergy, and repeating it in a prompt
        // buys nothing.
        var profile = Profile();

        profile.SetExcludedFoods(["Peanuts", "peanuts ", "PEANUTS"]);

        profile.ExcludedFoods.Should().ContainSingle().Which.Should().Be("Peanuts");
    }

    [Fact]
    public void AVeryLongEntryIsCut()
    {
        var profile = Profile();

        profile.SetExcludedFoods([new string('x', 500)]);

        profile.ExcludedFoods.Single().Length.Should().Be(60);
    }

    [Fact]
    public void TheListIsCapped()
    {
        // A prompt carries every one of these; an unbounded list is an unbounded prompt.
        var profile = Profile();

        profile.SetExcludedFoods(Enumerable.Range(0, 200).Select(i => $"food-{i}"));

        profile.ExcludedFoods.Should().HaveCount(50);
    }

    [Fact]
    public void SettingAnEmptyListClearsIt()
    {
        var profile = Profile();
        profile.SetExcludedFoods(["peanuts"]);

        profile.SetExcludedFoods([]);

        profile.ExcludedFoods.Should().BeEmpty();
    }
}
