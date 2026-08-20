using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class FilterConfiguration : IEntityTypeConfiguration<Filter>
{
    public void Configure(EntityTypeBuilder<Filter> builder)
    {
        builder.ToTable("Filters");
        builder.HasKey(filter => filter.Id);
        builder.Property(filter => filter.Pattern).HasMaxLength(500).IsRequired();
        builder.HasIndex(filter => filter.TaskId);

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(filter => filter.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
