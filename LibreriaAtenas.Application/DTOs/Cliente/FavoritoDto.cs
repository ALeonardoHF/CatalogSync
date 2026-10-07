namespace LibreriaAtenas.Application.DTOs.Cliente;

public record FavoritoDto(
    Guid Id,
    Guid LibroId,
    string ISBN,
    string Titulo,
    string Autor,
    string Editorial,
    decimal PrecioVenta,
    int? Existencia,
    string? EstadoInventario,
    string? Portada,
    DateTime CreadoEn);
