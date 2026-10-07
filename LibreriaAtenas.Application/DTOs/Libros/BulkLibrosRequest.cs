namespace LibreriaAtenas.Application.DTOs.Libros;

public record BulkLibrosRequest(List<Guid> Ids, string Accion); // Accion: "activar" | "desactivar"

public record BulkLibrosResult(int Afectados, int Errores);
