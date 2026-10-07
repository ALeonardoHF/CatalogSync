namespace CatalogSync.Application.Models;

public class ResumenProceso
{
    public int TotalCatalogo { get; set; }
    public int Actualizados { get; set; }
    public int SinCambio { get; set; }
    public int Nuevos { get; set; }
    public int Revisar { get; set; }
    public int IsbnInvalidos { get; set; }
    public string ArchivoId { get; set; } = "";
    public List<DetalleCambio> Cambios { get; set; } = [];
}

public class DetalleCambio
{
    public string ISBN { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public string Resultado { get; set; } = "";
    public string Detalle { get; set; } = "";
}
