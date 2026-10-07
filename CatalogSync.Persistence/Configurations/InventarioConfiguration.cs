using CatalogSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogSync.Persistence.Configurations;

public class InventarioConfiguration : IEntityTypeConfiguration<Inventario>
{
    public void Configure(EntityTypeBuilder<Inventario> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.Estado).IsRequired();
    }
}
