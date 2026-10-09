using CatalogSync.Api.Extensions;
using CatalogSync.Application.Models;
using CatalogSync.Application.DTOs.Libros;
using CatalogSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CatalogSync.Domain.Enums;

namespace CatalogSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CatalogoController : ControllerBase
{
    private const long MaxArchivoBytes = 30 * 1024 * 1024; // 30 MB por archivo

    private readonly IExcelService _excelService;
    private readonly ICatalogoService _catalogoService;
    private readonly IArchivoTemporalService _archivoTemporal;
    private readonly ILibroService _libroService;

    public CatalogoController(
        IExcelService excelService,
        ICatalogoService catalogoService,
        IArchivoTemporalService archivoTemporal,
        ILibroService libroService)
    {
        _excelService    = excelService;
        _catalogoService = catalogoService;
        _archivoTemporal = archivoTemporal;
        _libroService    = libroService;
    }

    [HttpPost("procesar")]
    [EnableRateLimiting("bulk")]
    public async Task<ActionResult<ResumenProceso>> Procesar(
        IFormFile existencias,
        [FromForm] string? existenciasHoja,
        [FromForm] string? estrategia)
    {
        if (existencias is null)
            return BadRequest("El archivo de existencias es requerido.");

        if (existencias.Length > MaxArchivoBytes)
            return BadRequest($"El archivo de existencias supera el límite de {MaxArchivoBytes / 1024 / 1024} MB.");
        
        var estrategiaPrecio = EstrategiaPrecio.MasAltoSiHayExistencia;
        if (!string.IsNullOrWhiteSpace(estrategia) && !Enum.TryParse(estrategia, ignoreCase: true, out estrategiaPrecio))
            return BadRequest($"Estrategia de precio inválida: '{estrategia}'.");

        var entradasFiles = Request.Form.Files
            .Where(f => f.Name == "entradas")
            .ToList();

        if (entradasFiles.Any(f => f.Length > MaxArchivoBytes))
            return BadRequest($"Uno o más archivos de entrada superan el límite de {MaxArchivoBytes / 1024 / 1024} MB.");

        var entradasHojas = Request.Form.TryGetValue("entradasHoja", out var hojaValues)
            ? hojaValues.Select(v => string.IsNullOrWhiteSpace(v) ? null : v).ToList()
            : new List<string?>();

        CatalogoLeido catalogoLeido;
        using (var stream = existencias.OpenReadStream())
            catalogoLeido = await _excelService.LeerExistenciasAsync(stream, existenciasHoja);

        var catalogo = catalogoLeido.Libros;

        var librosEntrada = new List<LibroProveedor>();
        for (int i = 0; i < entradasFiles.Count; i++)
        {
            var file  = entradasFiles[i];
            var hoja  = i < entradasHojas.Count ? entradasHojas[i] : null;
            var nombre = Path.GetFileNameWithoutExtension(file.FileName).ToUpperInvariant();

            using var stream = file.OpenReadStream();
            var libros = await _excelService.LeerProveedorAsync(stream, nombre, hoja);
            librosEntrada.AddRange(libros);
        }

        var resumen = _catalogoService.ProcesarCatalogo(catalogo, librosEntrada, estrategiaPrecio);

        var excelBytes = await _excelService.GenerarExcelActualizadoAsync(catalogo, catalogoLeido.Encabezados);
        resumen.ArchivoId = _archivoTemporal.Guardar(excelBytes);

        return Ok(resumen);
    }

    [HttpPost("importar-bd")]
    [EnableRateLimiting("bulk")]
    public async Task<ActionResult<ImportarCatalogoResult>> ImportarBd(
        IFormFile existencias,
        [FromForm] string? existenciasHoja,
        [FromForm] string? estrategia)
    {
        if (existencias is null)
            return BadRequest("El archivo de existencias es requerido.");

        if (existencias.Length > MaxArchivoBytes)
            return BadRequest($"El archivo supera el límite de {MaxArchivoBytes / 1024 / 1024} MB.");

        var estrategiaPrecio = EstrategiaPrecio.MasAltoSiHayExistencia;
        if (!string.IsNullOrWhiteSpace(estrategia) && !Enum.TryParse(estrategia, ignoreCase: true, out estrategiaPrecio))
            return BadRequest($"Estrategia de precio inválida: '{estrategia}'.");

        var entradasFiles = Request.Form.Files
            .Where(f => f.Name == "entradas")
            .ToList();

        if (entradasFiles.Any(f => f.Length > MaxArchivoBytes))
            return BadRequest($"Uno o más archivos de entrada superan el límite de {MaxArchivoBytes / 1024 / 1024} MB.");

        var entradasHojas = Request.Form.TryGetValue("entradasHoja", out var hojaValues)
            ? hojaValues.Select(v => string.IsNullOrWhiteSpace(v) ? null : v).ToList()
            : new List<string?>();

        List<LibroExistencia> catalogo;
        using (var stream = existencias.OpenReadStream())
            catalogo = (await _excelService.LeerExistenciasAsync(stream, existenciasHoja)).Libros;

        var librosEntrada = new List<LibroProveedor>();
        for (int i = 0; i < entradasFiles.Count; i++)
        {
            var file  = entradasFiles[i];
            var hoja  = i < entradasHojas.Count ? entradasHojas[i] : null;
            var nombre = Path.GetFileNameWithoutExtension(file.FileName).ToUpperInvariant();

            using var stream = file.OpenReadStream();
            var libros = await _excelService.LeerProveedorAsync(stream, nombre, hoja);
            librosEntrada.AddRange(libros);
        }

        _catalogoService.ProcesarCatalogo(catalogo, librosEntrada, estrategiaPrecio);

        // Solo se persisten los libros que algun proveedor mencionó en
        // este lote. El catalogo de existencias.xls puede tener decenas
        // de miles de filas que nadie tocó — reimportarlas todas pisaría
        // con el valor (quizá desactualizado) del Excel cualquier precio
        // o existencia que ya se haya ajustado directo en la base desde
        // la última vez que se exportó ese archivo.
        var isbnsProveedor = librosEntrada
            .Where(l => !string.IsNullOrWhiteSpace(l.ISBN))
            .Select(l => l.ISBN)
            .ToHashSet();

        var adminId = User.GetUserId();
        var items = catalogo
            .Where(l => isbnsProveedor.Contains(l.ISBN))
            .Select(l => new LibroImportItem(
                l.ISBN,
                l.Titulo,
                l.Autor,
                l.Editorial,
                l.Precio,
                ParseDecimal(l.Costo),
                string.IsNullOrWhiteSpace(l.CodigoBarra) ? null : l.CodigoBarra,
                ParseInt(l.Existencia),
                ParseInt(l.Ventas),
                l.FilaOriginal == 0));

        var result = await _libroService.ImportarCatalogoAsync(items, adminId, estrategiaPrecio);
        return Ok(result);
    }

    [HttpPost("importar-archivo")]
    [EnableRateLimiting("bulk")]
    public async Task<ActionResult<ImportarCatalogoResult>> ImportarArchivo(
        IFormFile archivo,
        [FromForm] string? hoja,
        [FromForm] string? proveedor,
        [FromForm] string? estrategia)
    {
        if (archivo is null)
            return BadRequest("El archivo es requerido.");

        if (archivo.Length > MaxArchivoBytes)
            return BadRequest($"El archivo supera el límite de {MaxArchivoBytes / 1024 / 1024} MB.");

        var estrategiaPrecio = EstrategiaPrecio.MasAltoSiHayExistencia;
        if (!string.IsNullOrWhiteSpace(estrategia) && !Enum.TryParse(estrategia, ignoreCase: true, out estrategiaPrecio))
            return BadRequest($"Estrategia de precio inválida: '{estrategia}'.");

        var nombreProveedor = !string.IsNullOrWhiteSpace(proveedor)
            ? proveedor.Trim().ToUpperInvariant()
            : Path.GetFileNameWithoutExtension(archivo.FileName).ToUpperInvariant();

        using var stream = archivo.OpenReadStream();
        var libros = await _excelService.LeerProveedorAsync(stream, nombreProveedor, hoja);

        if (libros.Count == 0)
            return BadRequest("No se encontraron registros válidos. Asegúrate de que el archivo tenga columnas ISBN y PRECIO.");

        var adminId = User.GetUserId();
        var items = libros.Select(l => new LibroImportItem(
            l.ISBN,
            string.IsNullOrWhiteSpace(l.Nombre) ? l.ISBN : l.Nombre,
            l.Autor,
            l.Editorial,
            l.PrecioUnitario,
            l.Costo,
            string.IsNullOrWhiteSpace(l.CodigoBarra) ? null : l.CodigoBarra,
            l.Existencia,
            l.Ventas,
            true));

        var result = await _libroService.ImportarCatalogoAsync(items, adminId, estrategiaPrecio);
        return Ok(result);
    }

    [HttpGet("descargar/{archivoId}")]
    public IActionResult Descargar(string archivoId)
    {
        if (!Guid.TryParse(archivoId, out _))
            return BadRequest("Identificador de archivo inválido.");

        var contenido = _archivoTemporal.Obtener(archivoId);
        if (contenido is null)
            return NotFound("Archivo no encontrado o expirado.");

        return File(contenido,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "existencias_actualizado.xlsx");
    }

    private static decimal ParseDecimal(string s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static int ParseInt(string s) =>
        int.TryParse(s, out var v) ? v : 0;
}
