using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class GroceryListConfiguration : IEntityTypeConfiguration<GroceryList>
{
    public void Configure(EntityTypeBuilder<GroceryList> builder)
    {
        builder.ToTable("grocery_lists");

        builder.HasKey(g => g.Id);

        // One list per user per range: asking for the same week twice returns the list that
        // is already being ticked off, not a second copy of it.
        builder.HasIndex(g => new { g.UserId, g.StartDate, g.EndDate }).IsUnique();

        builder.Property(g => g.Fingerprint).HasMaxLength(64);

        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Items)
            .WithOne(i => i.GroceryList)
            .HasForeignKey(i => i.GroceryListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GroceryListItemConfiguration : IEntityTypeConfiguration<GroceryListItem>
{
    public void Configure(EntityTypeBuilder<GroceryListItem> builder)
    {
        builder.ToTable("grocery_list_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Quantity).HasMaxLength(100);
        builder.Property(i => i.Category).HasMaxLength(60);
        builder.Property(i => i.Source).HasMaxLength(500);

        builder.HasIndex(i => new { i.GroceryListId, i.SortOrder });
    }
}
