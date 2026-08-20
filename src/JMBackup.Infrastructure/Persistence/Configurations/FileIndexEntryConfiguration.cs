using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class FileIndexEntryConfiguration : IEntityTypeConfiguration<FileIndexEntry>
{
    public void Configure(EntityTypeBuilder<FileIndexEntry> builder)
    {
        builder.ToTable("FileIndex");
        builder.HasKey(entry => new { entry.TaskName, entry.RelativePath });
        builder.Property(entry => entry.TaskName).HasMaxLength(200);
        builder.Property(entry => entry.RelativePath).HasMaxLength(1024);
        builder.Property(entry => entry.Sha256).HasMaxLength(64);
        builder.HasIndex(entry => entry.TaskName);
    }
}
