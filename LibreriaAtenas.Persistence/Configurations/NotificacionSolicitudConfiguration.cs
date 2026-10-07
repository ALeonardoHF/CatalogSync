using LibreriaAtenas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibreriaAtenas.Persistence.Configurations;

public class NotificacionSolicitudConfiguration : IEntityTypeConfiguration<NotificacionSolicitud>
{
    public void Configure(EntityTypeBuilder<NotificacionSolicitud> b)
    {
        b.HasKey(n => n.Id);
        b.HasIndex(n => new { n.UsuarioId, n.LibroId, n.Estado });
        b.Property(n => n.Estado).IsRequired();
    }
}
