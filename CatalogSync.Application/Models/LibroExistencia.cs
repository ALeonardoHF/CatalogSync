namespace CatalogSync.Application.Models;

public class LibroExistencia
{
    public string ISBN { get; set; } = "";
    public string ISBNOriginal { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Descuento { get; set; } = "";
    public string Editorial { get; set; } = "";
    public string Sello { get; set; } = "";
    public string Costo { get; set; } = "";
    public string Inc { get; set; } = "";
    public decimal Precio { get; set; }
    public string FechaEntrada { get; set; } = "";
    public string CodigoBarra { get; set; } = "";
    public string Existencia { get; set; } = "";
    public string Ventas { get; set; } = "";
    public int FilaOriginal { get; set; }
}
