using Foodeez.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foodeez.Infrastructure.Data.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(100);
        builder.Property(x => x.Value).HasMaxLength(4000);
        builder.Property(x => x.Category).HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
