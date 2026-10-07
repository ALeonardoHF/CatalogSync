using LibreriaAtenas.Application.DTOs.Cliente;
using LibreriaAtenas.Application.Interfaces;
using LibreriaAtenas.Domain.Entities;
using LibreriaAtenas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibreriaAtenas.Infrastructure.Services;

public class FavoritoService(LibreriaDbContext db) : IFavoritoService
{
    public async Task<IReadOnlyList<FavoritoDto>> GetMisFavoritosAsync(Guid usuarioId)
    {
        return await db.Favoritos
            .Include(f => f.Libro).ThenInclude(l => l.Inventario)
            .Where(f => f.UsuarioId == usuarioId)
            .OrderByDescending(f => f.CreadoEn)
            .Select(f => new FavoritoDto(
                f.Id, f.LibroId,
                f.Libro.ISBN, f.Libro.Titulo, f.Libro.Autor, f.Libro.Editorial,
                f.Libro.PrecioVenta,
                f.Libro.Inventario != null ? f.Libro.Inventario.Existencia : (int?)null,
                f.Libro.Inventario != null ? f.Libro.Inventario.Estado.ToString() : null,
                f.Libro.Portada,
                f.CreadoEn))
            .ToListAsync();
    }

    public async Task<Guid> AgregarAsync(Guid usuarioId, Guid libroId)
    {
        if (!await db.Libros.AnyAsync(l => l.Id == libroId && l.IsActive))
            throw new KeyNotFoundException("Libro no encontrado.");

        if (await db.Favoritos.AnyAsync(f => f.UsuarioId == usuarioId && f.LibroId == libroId))
            throw new InvalidOperationException("Ya está en favoritos.");

        var favorito = Favorito.Create(usuarioId, libroId);
        db.Favoritos.Add(favorito);
        await db.SaveChangesAsync();

        return favorito.Id;
    }

    public async Task QuitarAsync(Guid usuarioId, Guid libroId)
    {
        var favorito = await db.Favoritos
            .FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.LibroId == libroId)
            ?? throw new KeyNotFoundException("Favorito no encontrado.");

        db.Favoritos.Remove(favorito);
        await db.SaveChangesAsync();
    }
}
