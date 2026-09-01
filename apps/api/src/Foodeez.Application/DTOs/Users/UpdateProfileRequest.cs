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

    public bool DarkMode { get; set; }
    public UnitSystem UnitSystem { get; set; }
}
