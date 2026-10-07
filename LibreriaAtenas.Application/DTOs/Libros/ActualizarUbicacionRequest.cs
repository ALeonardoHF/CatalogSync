using LibreriaAtenas.Domain.Enums;

namespace LibreriaAtenas.Application.DTOs.Libros;

public record ActualizarUbicacionRequest(
    TipoUbicacion Tipo,
    string? Seccion,
    string? Estante,
    string? Referencia,
    string? Notas);
