namespace LibreriaAtenas.Application.DTOs.Libros;

public record ActualizarPrecioRequest(decimal PrecioVenta, decimal? Costo, string? Fuente);
