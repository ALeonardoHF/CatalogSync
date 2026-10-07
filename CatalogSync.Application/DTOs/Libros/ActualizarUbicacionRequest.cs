using CatalogSync.Domain.Enums;

namespace CatalogSync.Application.DTOs.Libros;

public record ActualizarUbicacionRequest(
    TipoUbicacion Tipo,
    string? Seccion,
    string? Estante,
    string? Referencia,
    string? Notas);
