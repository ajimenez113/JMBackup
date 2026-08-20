using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("Schedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Times).HasMaxLength(500).IsRequired();
        builder.Property(schedule => schedule.Weekdays).HasMaxLength(50);
        builder.Property(schedule => schedule.MonthDays).HasMaxLength(200);
        builder.Property(schedule => schedule.CatchupPolicy).HasMaxLength(50);
        builder.HasIndex(schedule => schedule.TaskId);

        builder.HasOne<TaskDefinition>()
            .WithMany()
            .HasForeignKey(schedule => schedule.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
