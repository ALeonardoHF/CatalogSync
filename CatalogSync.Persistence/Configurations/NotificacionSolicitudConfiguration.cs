using CatalogSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogSync.Persistence.Configurations;

public class NotificacionSolicitudConfiguration : IEntityTypeConfiguration<NotificacionSolicitud>
{
    public void Configure(EntityTypeBuilder<NotificacionSolicitud> b)
    {
        b.HasKey(n => n.Id);

        // Para listar las solicitudes de un usuario (todas, sin filtrar
        // por estado).
        b.HasIndex(n => n.UsuarioId);

        // Solo puede existir una solicitud Pendiente (1) por usuario y
        // libro — antes esto solo lo validaba el AnyAsync en
        // NotificacionService, lo cual tiene una condición de carrera
        // real: dos solicitudes simultáneas pueden pasar esa validación
        // antes de que ninguna se guarde todavía. El índice único filtrado
        // lo garantiza también a nivel de base de datos.
        b.HasIndex(n => new { n.UsuarioId, n.LibroId })
         .IsUnique()
         .HasFilter("[Estado] = 1")
         .HasDatabaseName("IX_NotificacionesSolicitud_UsuarioId_LibroId_Pendiente");

        b.Property(n => n.Estado).IsRequired();
    }
}
