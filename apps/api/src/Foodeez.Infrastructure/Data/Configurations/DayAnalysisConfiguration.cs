using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class DayAnalysisConfiguration : IEntityTypeConfiguration<DayAnalysis>
{
    public void Configure(EntityTypeBuilder<DayAnalysis> builder)
    {
        builder.ToTable("day_analyses");

        builder.HasKey(d => d.Id);

        // One analysis per user per day; a regenerated one replaces the row rather than adding to it.
        builder.HasIndex(d => new { d.UserId, d.LogDate }).IsUnique();

        builder.Property(d => d.Status).HasMaxLength(1000);
        builder.Property(d => d.Fingerprint).HasMaxLength(64);

        builder.Property(d => d.Gaps)
            .HasColumnType("text")
            .HasConversion(JsonStringList.Converter, JsonStringList.Comparer);

        builder.Property(d => d.Recommendations)
            .HasColumnType("text")
            .HasConversion(JsonStringList.Converter, JsonStringList.Comparer);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
