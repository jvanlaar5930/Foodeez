using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.Users;

public class UpdateProfileRequest
{
    [Range(50, 300)]
    public float HeightCm { get; set; }

    [Range(20, 500)]
    public float WeightKg { get; set; }

    public float? TargetWeightKg { get; set; }

    [Range(1, 120)]
    public int Age { get; set; }

    public Gender Gender { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public DietaryGoal DietaryGoal { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Foods to exclude from AI meal plans and suggestions. Null leaves the stored list alone -
    /// a client that does not know about this field must not be able to clear someone's
    /// allergies by omitting it. An empty list clears them deliberately.
    /// </summary>
    public List<string>? ExcludedFoods { get; set; }

    public bool DarkMode { get; set; }
    public UnitSystem UnitSystem { get; set; }
}
