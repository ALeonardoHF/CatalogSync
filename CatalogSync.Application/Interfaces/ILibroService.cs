using CatalogSync.Application.DTOs.Libros;
using CatalogSync.Domain.Enums;

namespace CatalogSync.Application.Interfaces;

public interface ILibroService
{
    Task<PagedResult<LibroDto>> BuscarAsync(string? q, int page, int pageSize, bool soloConExistencia = false, bool? isActive = null, bool sinExistencia = false);
    Task<LibroDto?> GetByIdAsync(Guid id);
    Task<LibroDto?> GetByIsbnAsync(string isbn);
    Task<LibroDto> CrearAsync(CrearLibroRequest request, Guid adminId);
    Task<LibroDto> ActualizarAsync(Guid id, ActualizarLibroRequest request);
    Task ActualizarPrecioAsync(Guid id, ActualizarPrecioRequest request, Guid adminId);
    Task ActualizarInventarioAsync(Guid id, ActualizarInventarioRequest request);
    Task ActualizarUbicacionAsync(Guid id, ActualizarUbicacionRequest request);
    Task DesactivarAsync(Guid id);
    Task ActivarAsync(Guid id);
    Task<IReadOnlyList<HistorialPrecioDto>> GetHistorialPreciosAsync(Guid libroId);
    Task<ImportarCatalogoResult> ImportarCatalogoAsync(IEnumerable<LibroImportItem> items, Guid adminId, EstrategiaPrecio estrategia = EstrategiaPrecio.MasAltoSiHayExistencia);
    Task<BulkLibrosResult> BulkAccionAsync(List<Guid> ids, string accion);
    Task<BulkLibrosResult> DesactivarAgotadosAsync();
    Task<string> ActualizarPortadaAsync(Guid id, string url);
    Task<BulkPortadasResult> BulkPortadasAsync(IReadOnlyList<(string Isbn, string Url, string Archivo)> portadas);
}
