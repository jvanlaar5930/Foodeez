using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.ToTable("food_items");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Brand)
            .HasMaxLength(200);

        builder.Property(f => f.ServingUnit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Category)
            .HasMaxLength(100);

        builder.Property(f => f.Barcode)
            .HasMaxLength(100);

        builder.HasIndex(f => f.Barcode);

        builder.Property(f => f.ServingSize).HasColumnType("float");

        // Owned NutritionalInfo with "nutrition_" column prefix
        builder.OwnsOne(f => f.NutritionalInfo, ni =>
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
