using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JMBackup.Infrastructure.Persistence.Configurations;

public sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credentials");
        builder.HasKey(credential => credential.Id);
        builder.Property(credential => credential.Alias).HasMaxLength(200).IsRequired();
        builder.Property(credential => credential.Username).HasMaxLength(200);
        builder.Property(credential => credential.EncryptedSecret).IsRequired();
    }
}
