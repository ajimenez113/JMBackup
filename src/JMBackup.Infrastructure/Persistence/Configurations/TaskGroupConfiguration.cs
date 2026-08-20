using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class TaskGroupConfiguration : IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.ToTable("TaskGroups");
        builder.HasKey(group => group.Id);
        builder.Property(group => group.Name).HasMaxLength(200).IsRequired();
    }
}
