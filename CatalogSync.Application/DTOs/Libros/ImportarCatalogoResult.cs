namespace CatalogSync.Application.DTOs.Libros;

public record ImportarCatalogoResult(
    int Creados,
    int PreciosActualizados,
    int SinCambio,
    int Errores,
    IReadOnlyList<string> MensajesError,
    int Revisar = 0,
    IReadOnlyList<string>? MensajesRevisar = null);
