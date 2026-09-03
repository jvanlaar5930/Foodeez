using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class MealTemplateConfiguration : IEntityTypeConfiguration<MealTemplate>
{
    public void Configure(EntityTypeBuilder<MealTemplate> builder)
    {
        builder.ToTable("meal_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Saved meals are looked up by owner and by name on every quick add, and a user may
        // not have two meals under one name - "my usual lunch" has to mean one thing.
        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.MealTemplate)
            .HasForeignKey(i => i.MealTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.TotalNutrition);
    }
}

public class MealTemplateItemConfiguration : IEntityTypeConfiguration<MealTemplateItem>
{
    public void Configure(EntityTypeBuilder<MealTemplateItem> builder)
    {
        builder.ToTable("meal_template_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Quantity).HasColumnType("float");

        // Restrict, not Cascade: deleting a food item that a saved meal is built from would
        // silently shrink the meal, and the user would log it again none the wiser.
        builder.HasOne(i => i.FoodItem)
            .WithMany()
            .HasForeignKey(i => i.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(i => i.NutritionalInfo, ni =>
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
