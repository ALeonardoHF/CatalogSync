namespace LibreriaAtenas.Application.DTOs.Libros;

public record ImportarCatalogoResult(
    int Creados,
    int PreciosActualizados,
    int SinCambio,
    int Errores,
    IReadOnlyList<string> MensajesError);
