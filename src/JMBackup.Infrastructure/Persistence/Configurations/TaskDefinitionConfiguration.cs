using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class TaskDefinitionConfiguration : IEntityTypeConfiguration<TaskDefinition>
{
    public void Configure(EntityTypeBuilder<TaskDefinition> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(task => task.Name).IsUnique();

        builder.HasOne<TaskGroup>()
            .WithMany()
            .HasForeignKey(task => task.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
