namespace CatalogSync.Application.DTOs.Libros;

public record BulkPortadasItem(string Archivo, string Isbn, string? Titulo, string Resultado);

public record BulkPortadasResult(int Asignadas, int NoEncontradas, int Errores, List<BulkPortadasItem> Detalles);
