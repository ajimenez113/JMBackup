using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class QueueItemConfiguration : IEntityTypeConfiguration<QueueItem>
{
    public void Configure(EntityTypeBuilder<QueueItem> builder)
    {
        builder.ToTable("QueueItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.RelativePath).HasMaxLength(1024).IsRequired();
        builder.HasIndex(item => new { item.TaskId, item.State });

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(item => item.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
