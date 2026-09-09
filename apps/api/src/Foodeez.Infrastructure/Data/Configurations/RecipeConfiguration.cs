using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        // TEXT, not varchar: recipe summaries from upstream routinely run well past a
        // thousand characters, and a varchar cap silently cuts them off mid-sentence.
        builder.Property(r => r.Description)
            .HasColumnType("text");

        builder.Property(r => r.Instructions)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.Tags)
            .HasMaxLength(500);

        builder.Property(r => r.ImageUrl)
            .HasMaxLength(500);

        builder.Property(r => r.SourceUrl)
            .HasMaxLength(500);

        builder.Property(r => r.SourceName)
            .HasMaxLength(200);

        builder.Property(r => r.EnhancementNotes)
            .HasColumnType("text");

        builder.HasMany(r => r.Ingredients)
            .WithOne(i => i.Recipe)
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // An enhancement points back at the recipe it elevates. Cascading is what stops a
        // deleted recipe leaving an enhancement of nothing behind, reachable by id but with
        // no original to compare it against.
        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(r => r.EnhancedFromRecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // At most one enhancement per recipe per person, enforced where it cannot be raced:
        // two clicks landing together would otherwise each find no enhancement and each write
        // one, and the reader would be left toggling between two "the" enhanced versions.
        // MySQL lets a unique index hold any number of NULL rows, so ordinary recipes - every
        // one of which has both columns null - are untouched by this.
        builder.HasIndex(r => new { r.EnhancedFromRecipeId, r.CreatedByUserId })
            .IsUnique()
            .HasDatabaseName("ix_recipes_enhanced_from_user");

        builder.OwnsOne(r => r.NutritionalInfoPerServing, ni =>
        {
            ni.Property(n => n.Calories).HasColumnName("nutrition_calories").HasColumnType("float");
            ni.Property(n => n.Protein).HasColumnName("nutrition_protein").HasColumnType("float");
            ni.Property(n => n.Carbohydrates).HasColumnName("nutrition_carbohydrates").HasColumnType("float");
            ni.Property(n => n.Fat).HasColumnName("nutrition_fat").HasColumnType("float");
            ni.Property(n => n.Fiber).HasColumnName("nutrition_fiber").HasColumnType("float");
            ni.Property(n => n.Sugar).HasColumnName("nutrition_sugar").HasColumnType("float");
            ni.Property(n => n.Sodium).HasColumnName("nutrition_sodium").HasColumnType("float");
        });
    }
}

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipe_ingredients");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Notes)
            .HasMaxLength(200);

        builder.Property(i => i.Quantity).HasColumnType("float");

        builder.HasOne(i => i.FoodItem)
            .WithMany()
            .HasForeignKey(i => i.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
