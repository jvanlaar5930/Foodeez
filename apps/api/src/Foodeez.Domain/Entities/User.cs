using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;

    public UserProfile? Profile { get; set; }
    public ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();

    private User() { }

    public static User Create(string email, string passwordHash, string firstName, string lastName)
    {
        return new User
        {
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };
    }
}
