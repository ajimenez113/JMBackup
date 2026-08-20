using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(setting => setting.Key);
        builder.Property(setting => setting.Key).HasMaxLength(200);
        builder.Property(setting => setting.ValueJson).IsRequired();
    }
}
