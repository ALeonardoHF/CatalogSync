using CatalogSync.Domain.Enums;

namespace CatalogSync.Domain.Entities;

public class NotificacionSolicitud
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid LibroId { get; private set; }
    public EstadoNotificacion Estado { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime? EnviadoEn { get; private set; }

    public Usuario Usuario { get; private set; } = null!;
    public Libro Libro { get; private set; } = null!;

    private NotificacionSolicitud() { }

    public static NotificacionSolicitud Create(Guid usuarioId, Guid libroId) =>
        new()
        {
            Id        = Guid.NewGuid(),
            UsuarioId = usuarioId,
            LibroId   = libroId,
            Estado    = EstadoNotificacion.Pendiente,
            CreadoEn  = DateTime.UtcNow
        };

    public void MarcarEnviada()
    {
        Estado    = EstadoNotificacion.Enviada;
        EnviadoEn = DateTime.UtcNow;
    }

    public void Cancelar() => Estado = EstadoNotificacion.Cancelada;
}
