namespace LibreriaAtenas.Application.DTOs.Libros;

public record CrearLibroRequest(
    string ISBN,
    string Titulo,
    string Autor,
    string Editorial,
    decimal PrecioVenta,
    decimal Costo,
    decimal Descuento = 0,
    string? Portada = null,
    string? Sinopsis = null,
    string? Genero = null,
    int? Paginas = null,
    int? AnioPublicacion = null,
    string? CodigoBarra = null);
