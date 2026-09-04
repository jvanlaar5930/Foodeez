using System.Reflection;
using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// This mapping was copied out by hand in five use cases and had drifted apart in exactly the
/// way hand-copied mappings do - quietly, and only in the fields nobody looked at. The
/// recommendations copy left out ExcludedFoods, so the one prompt whose whole job is dietary
/// advice was the only one that never heard about the user's allergies.
///
/// The completeness test below is the one that matters: it fails when a property is added to
/// the DTO and not to the mapper, which is the failure mode that produced the bug.
/// </summary>
public class UserProfileMapperTests
{
    private static UserProfile FullyPopulated() => new()
    {
        UserId = Guid.NewGuid(),
        HeightCm = 180,
        WeightKg = 80,
        TargetWeightKg = 75,
        Age = 30,
        Gender = Gender.Female,
        ActivityLevel = ActivityLevel.VeryActive,
        DietaryGoal = DietaryGoal.MuscleGain,
        DailyCalorieTarget = 2600,
        DailyProteinTargetG = 96,
        DailyCarbTargetG = 306,
        DailyFatTargetG = 86,
        Notes = "prefers one-pan dinners",
        ExcludedFoods = new List<string> { "peanuts", "shellfish" },
        ProfileCompleted = true,
        DarkMode = true,
        UnitSystem = UnitSystem.Metric
    };

    [Fact]
    public void ToDto_CarriesEveryProperty()
    {
        // Reflection rather than 17 assertions: a new property has to be handled here, and an
        // assertion list would silently not cover it.
        var profile = FullyPopulated();

        var dto = UserProfileMapper.ToDto(profile);

        foreach (var dtoProperty in typeof(UserProfileDto).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var source = typeof(UserProfile).GetProperty(dtoProperty.Name);
            source.Should().NotBeNull(
                $"UserProfileDto.{dtoProperty.Name} has no UserProfile property to map from - " +
                "either name them alike or exclude it here deliberately");

            var expected = source!.GetValue(profile);
            var actual = dtoProperty.GetValue(dto);

            actual.Should().BeEquivalentTo(expected,
                $"UserProfileMapper.ToDto does not copy {dtoProperty.Name}");
        }
    }

    [Fact]
    public void ToDto_CopiesExcludedFoods_SoAdviceRespectsAllergies()
    {
        var profile = FullyPopulated();

        UserProfileMapper.ToDto(profile).ExcludedFoods
            .Should().Equal("peanuts", "shellfish");
    }

    [Fact]
    public void ToDto_CopiesTheExclusionList_RatherThanSharingIt()
    {
        // A prompt builder that appended to the list it was handed would otherwise be editing
        // the tracked entity, and the change would be saved on the next SaveChanges.
        var profile = FullyPopulated();

        var dto = UserProfileMapper.ToDto(profile);
        dto.ExcludedFoods.Add("mushrooms");

        profile.ExcludedFoods.Should().Equal("peanuts", "shellfish");
    }

    [Fact]
    public void ToDtoOrNull_IsNullForAUserWhoHasNotFilledOneIn()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User");

        UserProfileMapper.ToDtoOrNull(user).Should().BeNull(
            because: "a profile of zeroes reads to the model as real measurements");
    }

    [Fact]
    public void ToDtoOrNull_MapsTheProfileWhenThereIsOne()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User");
        user.Profile = FullyPopulated();

        UserProfileMapper.ToDtoOrNull(user)!.ExcludedFoods.Should().Equal("peanuts", "shellfish");
    }
}
