using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class ExclusionConfiguration : IEntityTypeConfiguration<Exclusion>
{
    public void Configure(EntityTypeBuilder<Exclusion> builder)
    {
        builder.ToTable("Exclusions");
        builder.HasKey(exclusion => exclusion.Id);
        builder.Property(exclusion => exclusion.Pattern).HasMaxLength(500);
        builder.HasIndex(exclusion => exclusion.TaskId);

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(exclusion => exclusion.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
