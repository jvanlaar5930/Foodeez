using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

/// <summary>
/// A recipe a user has kept. Deliberately a join row rather than a flag on the recipe:
/// recipes are shared across every account, so ownership of the bookmark has to live
/// somewhere the next user's save cannot overwrite.
/// </summary>
public class SavedRecipe : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }

    /// <summary>Drives the newest-first ordering of the saved deck.</summary>
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Recipe? Recipe { get; set; }
}
