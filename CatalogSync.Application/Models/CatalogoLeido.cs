namespace CatalogSync.Application.Models;

/// <summary>
/// Resultado de leer el archivo de existencias: el catálogo parseado más los
/// encabezados originales, para poder regenerar el Excel de salida sin volver
/// a abrir ni reparsear el archivo original.
/// </summary>
public record CatalogoLeido(List<LibroExistencia> Libros, string[] Encabezados);
