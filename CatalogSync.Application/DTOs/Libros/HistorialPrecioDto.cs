namespace CatalogSync.Application.DTOs.Libros;

public record HistorialPrecioDto(
    Guid Id,
    decimal PrecioAnterior,
    decimal PrecioNuevo,
    string Fuente,
    Guid? CambiadoPorId,
    DateTime CambiadoEn);
