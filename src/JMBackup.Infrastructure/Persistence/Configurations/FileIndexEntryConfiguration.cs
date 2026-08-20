using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class FileIndexEntryConfiguration : IEntityTypeConfiguration<FileIndexEntry>
{
    public void Configure(EntityTypeBuilder<FileIndexEntry> builder)
    {
        builder.ToTable("FileIndex");
        builder.HasKey(entry => new { entry.TaskId, entry.RelativePath });
        builder.Property(entry => entry.RelativePath).HasMaxLength(1024);
        builder.Property(entry => entry.Sha256).HasMaxLength(64);

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(entry => entry.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
