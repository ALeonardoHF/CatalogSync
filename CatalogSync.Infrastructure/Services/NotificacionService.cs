using CatalogSync.Application.DTOs.Cliente;
using CatalogSync.Application.Interfaces;
using CatalogSync.Domain.Entities;
using CatalogSync.Infrastructure.Utilidades;
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

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad())
        {
            // El AnyAsync de arriba no evita una condición de carrera real:
            // dos solicitudes simultáneas pueden pasar esa validación antes
            // de que ninguna se haya guardado todavía. El índice único
            // filtrado en la base de datos es la última línea de defensa.
            throw new InvalidOperationException("Ya tienes una solicitud pendiente para este libro.");
        }

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
