namespace CatalogSync.Application.DTOs.Libros;

public record ActualizarLibroRequest(
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
    string? CodigoBarra);
