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

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.Instructions)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.Tags)
            .HasMaxLength(500);

        builder.Property(r => r.ImageUrl)
            .HasMaxLength(500);

        builder.HasMany(r => r.Ingredients)
            .WithOne(i => i.Recipe)
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

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
