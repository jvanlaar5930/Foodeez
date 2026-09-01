using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class SavedRecipeConfiguration : IEntityTypeConfiguration<SavedRecipe>
{
    public void Configure(EntityTypeBuilder<SavedRecipe> builder)
    {
        builder.ToTable("saved_recipes");

        builder.HasKey(s => s.Id);

        // Saving twice is a no-op, not a second row. Enforced here as well as in the use case
        // so a double-tap that races itself cannot leave a duplicate behind.
        builder.HasIndex(s => new { s.UserId, s.RecipeId }).IsUnique();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Recipe)
            .WithMany()
            .HasForeignKey(s => s.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
