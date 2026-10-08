namespace CatalogSync.Domain.Servicios;

public static class RevisionDeCampos
{
    /// <summary>
    /// Regla compartida de reconciliacion de metadatos (titulo, autor,
    /// editorial, sello): si el valor actual esta vacio, se completa con
    /// el nuevo. Si ambos tienen valor y son distintos, se registra la
    /// diferencia para revision manual — nunca se sobrescribe un valor
    /// que ya existe. La usan tanto el reporte de Excel (CatalogoService)
    /// como la importacion a la base de datos (LibroService).
    /// </summary>
    public static void Revisar(List<string> diferencias, string campo, string actual, string nuevo, Action<string> completar)
    {
        if (string.IsNullOrWhiteSpace(nuevo)) return;

        if (string.IsNullOrWhiteSpace(actual))
        {
            completar(nuevo);
            return;
        }

        if (!string.Equals(actual, nuevo, StringComparison.OrdinalIgnoreCase))
            diferencias.Add($"{campo}: '{actual}' ≠ '{nuevo}'");
    }
}
