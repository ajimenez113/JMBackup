using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class TaskPathConfiguration : IEntityTypeConfiguration<TaskPath>
{
    public void Configure(EntityTypeBuilder<TaskPath> builder)
    {
        builder.ToTable("TaskPaths");
        builder.HasKey(path => path.Id);
        builder.Property(path => path.Path).HasMaxLength(1024).IsRequired();
        builder.Property(path => path.Region).HasMaxLength(64);
        builder.Property(path => path.StorageClass).HasMaxLength(64);
        builder.HasIndex(path => path.TaskId);

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(path => path.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Credential>()
            .WithMany()
            .HasForeignKey(path => path.CredentialId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
