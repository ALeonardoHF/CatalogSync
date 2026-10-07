using CatalogSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogSync.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasKey(r => r.Id);
        b.Property(r => r.Token).IsRequired().HasMaxLength(500);
        b.HasIndex(r => r.Token).IsUnique();
        b.Property(r => r.DireccionIp).HasMaxLength(50);
    }
}
