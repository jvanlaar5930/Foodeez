namespace Foodeez.Domain.ValueObjects;

public class NutritionalInfo
{
    public float Calories { get; private set; }
    public float Protein { get; private set; }
    public float Carbohydrates { get; private set; }
    public float Fat { get; private set; }
    public float Fiber { get; private set; }
    public float Sugar { get; private set; }
    public float Sodium { get; private set; }

    // Required by EF Core
    private NutritionalInfo() { }

    public NutritionalInfo(float calories, float protein, float carbohydrates, float fat, float fiber, float sugar, float sodium)
    {
        Calories = calories;
        Protein = protein;
        Carbohydrates = carbohydrates;
        Fat = fat;
        Fiber = fiber;
        Sugar = sugar;
        Sodium = sodium;
    }

    public static NutritionalInfo Empty => new NutritionalInfo(0f, 0f, 0f, 0f, 0f, 0f, 0f);

    /// <summary>
    /// Returns a new NutritionalInfo with all values multiplied by the given factor.
    /// Used to scale nutrition values to a specific quantity/serving.
    /// </summary>
    public NutritionalInfo Scale(float factor)
    {
        return new NutritionalInfo(
            Calories * factor,
            Protein * factor,
            Carbohydrates * factor,
            Fat * factor,
            Fiber * factor,
            Sugar * factor,
            Sodium * factor
        );
    }

    public static NutritionalInfo operator +(NutritionalInfo a, NutritionalInfo b)
    {
        return new NutritionalInfo(
            a.Calories + b.Calories,
            a.Protein + b.Protein,
            a.Carbohydrates + b.Carbohydrates,
            a.Fat + b.Fat,
            a.Fiber + b.Fiber,
            a.Sugar + b.Sugar,
            a.Sodium + b.Sodium
        );
    }
}
