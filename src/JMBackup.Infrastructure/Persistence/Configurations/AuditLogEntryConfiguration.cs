using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Actor).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.SourceIp).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(100).IsRequired();
        builder.HasIndex(entry => entry.Timestamp);
    }
}
