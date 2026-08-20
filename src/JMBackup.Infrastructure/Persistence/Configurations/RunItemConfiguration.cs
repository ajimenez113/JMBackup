using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class RunItemConfiguration : IEntityTypeConfiguration<RunItem>
{
    public void Configure(EntityTypeBuilder<RunItem> builder)
    {
        builder.ToTable("RunItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Path).HasMaxLength(1024).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(100);

        // Índice explícito del modelo de datos (01-ARQUITECTURA.md §4): el historial
        // filtra "Respaldados" vs "Errores" por (RunId, Status) constantemente (RF-131).
        builder.HasIndex(item => new { item.RunId, item.Status });

        builder.HasOne<Run>()
            .WithMany()
            .HasForeignKey(item => item.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
