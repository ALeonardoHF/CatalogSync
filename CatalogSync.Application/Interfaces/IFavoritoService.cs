using CatalogSync.Application.DTOs.Cliente;

namespace CatalogSync.Application.Interfaces;

public interface IFavoritoService
{
    Task<IReadOnlyList<FavoritoDto>> GetMisFavoritosAsync(Guid usuarioId);
    Task<Guid> AgregarAsync(Guid usuarioId, Guid libroId);
    Task QuitarAsync(Guid usuarioId, Guid libroId);
}
