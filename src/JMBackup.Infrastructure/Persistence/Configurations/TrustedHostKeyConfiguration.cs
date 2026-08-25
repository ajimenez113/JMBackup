using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class TrustedHostKeyConfiguration : IEntityTypeConfiguration<TrustedHostKey>
{
    public void Configure(EntityTypeBuilder<TrustedHostKey> builder)
    {
        builder.ToTable("TrustedHostKeys");
        builder.HasKey(hostKey => hostKey.Id);
        builder.Property(hostKey => hostKey.Host).HasMaxLength(300).IsRequired();
        builder.Property(hostKey => hostKey.Algorithm).HasMaxLength(50).IsRequired();
        builder.Property(hostKey => hostKey.Fingerprint).HasMaxLength(200).IsRequired();
        builder.HasIndex(hostKey => new { hostKey.Host, hostKey.Port }).IsUnique();
    }
}
