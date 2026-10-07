namespace CatalogSync.Application.DTOs.Libros;

public record LibroDto(
    Guid Id,
    string ISBN,
    string Titulo,
    string Autor,
    string Editorial,
    decimal PrecioVenta,
    decimal Costo,
    decimal Descuento,
    string? Portada,
    string? Sinopsis,
    string? Genero,
    int? Paginas,
    int? AnioPublicacion,
    string? CodigoBarra,
    bool IsActive,
    DateTime CreadoEn,
    // inventario inline
    int? Existencia,
    int? Ventas,
    string? EstadoInventario,
    // ubicacion inline
    string? TipoUbicacion,
    string? Seccion,
    string? Estante,
    string? Referencia);
