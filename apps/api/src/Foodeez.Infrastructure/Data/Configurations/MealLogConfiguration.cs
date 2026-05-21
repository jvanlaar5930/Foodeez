using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class MealLogConfiguration : IEntityTypeConfiguration<MealLog>
{
    public void Configure(EntityTypeBuilder<MealLog> builder)
    {
        builder.ToTable("meal_logs");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        builder.HasIndex(m => new { m.UserId, m.LogDate });

        builder.HasMany(m => m.Items)
            .WithOne(i => i.MealLog)
            .HasForeignKey(i => i.MealLogId)
            .OnDelete(DeleteBehavior.Cascade);

        // TotalNutrition is a computed property — ignore it
        builder.Ignore(m => m.TotalNutrition);
    }
}

public class MealLogItemConfiguration : IEntityTypeConfiguration<MealLogItem>
{
    public void Configure(EntityTypeBuilder<MealLogItem> builder)
    {
        builder.ToTable("meal_log_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Quantity).HasColumnType("float");

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
