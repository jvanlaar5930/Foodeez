namespace Foodeez.Domain.Enums;

/// <summary>
/// Represents the physical activity level of a user.
/// </summary>
public enum ActivityLevel
{
    /// <summary>
    /// Sedentary lifestyle: desk job, little or no exercise.
    /// BMR multiplier: 1.2
    /// </summary>
    Sedentary,

    /// <summary>
    /// Lightly active: light exercise or sports 1-3 days per week.
    /// BMR multiplier: 1.375
    /// </summary>
    LightlyActive,

    /// <summary>
    /// Moderately active: moderate exercise or sports 3-5 days per week.
    /// BMR multiplier: 1.55
    /// </summary>
    ModeratelyActive,

    /// <summary>
    /// Very active: hard exercise or sports 6-7 days per week.
    /// BMR multiplier: 1.725
    /// </summary>
    VeryActive,

    /// <summary>
    /// Extra active: very hard exercise, physical job, or training twice per day.
    /// BMR multiplier: 1.9
    /// </summary>
    ExtraActive
}
