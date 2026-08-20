using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class RunConfiguration : IEntityTypeConfiguration<Run>
{
    public void Configure(EntityTypeBuilder<Run> builder)
    {
        builder.ToTable("Runs");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.CorrelationId).HasMaxLength(64).IsRequired();
        builder.HasIndex(run => run.TaskId);
        builder.HasIndex(run => run.StartedAt);

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(run => run.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
