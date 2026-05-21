using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("meal_plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(p => p.Entries)
            .WithOne(e => e.MealPlan)
            .HasForeignKey(e => e.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("meal_plan_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.Servings).HasColumnType("float");

        builder.HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.FoodItem)
            .WithMany()
            .HasForeignKey(e => e.FoodItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
