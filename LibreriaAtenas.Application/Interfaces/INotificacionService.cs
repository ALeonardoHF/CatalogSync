using LibreriaAtenas.Application.DTOs.Cliente;

namespace LibreriaAtenas.Application.Interfaces;

public interface INotificacionService
{
    Task<IReadOnlyList<NotificacionDto>> GetMisSolicitudesAsync(Guid usuarioId);
    Task<Guid> SolicitarAsync(Guid usuarioId, Guid libroId);
    Task CancelarAsync(Guid solicitudId, Guid usuarioId);
}
