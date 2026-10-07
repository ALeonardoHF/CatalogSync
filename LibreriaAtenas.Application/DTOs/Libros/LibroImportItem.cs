namespace LibreriaAtenas.Application.DTOs.Libros;

public record LibroImportItem(
    string ISBN,
    string Titulo,
    string Autor,
    string Editorial,
    decimal PrecioVenta,
    decimal Costo,
    string? CodigoBarra,
    int Existencia,
    int Ventas,
    bool EsNuevo);
