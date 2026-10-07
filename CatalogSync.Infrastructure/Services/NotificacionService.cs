using CatalogSync.Application.DTOs.Cliente;
using CatalogSync.Application.Interfaces;
using CatalogSync.Domain.Entities;
using CatalogSync.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogSync.Infrastructure.Services;

public class NotificacionService(LibreriaDbContext db) : INotificacionService
{
    public async Task<IReadOnlyList<NotificacionDto>> GetMisSolicitudesAsync(Guid usuarioId)
    {
        return await db.NotificacionesSolicitud
            .Include(n => n.Libro).ThenInclude(l => l.Inventario)
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.CreadoEn)
            .Select(n => new NotificacionDto(
                n.Id, n.LibroId,
                n.Libro.ISBN, n.Libro.Titulo, n.Libro.Autor,
                n.Libro.Inventario != null ? n.Libro.Inventario.Existencia : (int?)null,
                n.Estado.ToString(),
                n.CreadoEn, n.EnviadoEn))
            .ToListAsync();
    }

    public async Task<Guid> SolicitarAsync(Guid usuarioId, Guid libroId)
    {
        if (!await db.Libros.AnyAsync(l => l.Id == libroId && l.IsActive))
            throw new KeyNotFoundException("Libro no encontrado.");

        var yaExiste = await db.NotificacionesSolicitud.AnyAsync(n =>
            n.UsuarioId == usuarioId &&
            n.LibroId   == libroId  &&
            n.Estado    == Domain.Enums.EstadoNotificacion.Pendiente);

        if (yaExiste)
            throw new InvalidOperationException("Ya tienes una solicitud pendiente para este libro.");

        var solicitud = NotificacionSolicitud.Create(usuarioId, libroId);
        db.NotificacionesSolicitud.Add(solicitud);
        await db.SaveChangesAsync();

        return solicitud.Id;
    }

    public async Task CancelarAsync(Guid solicitudId, Guid usuarioId)
    {
        var solicitud = await db.NotificacionesSolicitud
            .FirstOrDefaultAsync(n => n.Id == solicitudId && n.UsuarioId == usuarioId)
            ?? throw new KeyNotFoundException("Solicitud no encontrada.");

        solicitud.Cancelar();
        await db.SaveChangesAsync();
    }
}
