namespace CatalogSync.Application.Models;

public class LibroProveedor
{
    public string ISBN { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Editorial { get; set; } = "";
    public string Sello { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public decimal Costo { get; set; }
    public string CodigoBarra { get; set; } = "";
    public int Existencia { get; set; }
    public int Ventas { get; set; }
    public string Proveedor { get; set; } = "";
}
