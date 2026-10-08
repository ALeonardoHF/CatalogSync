using System.Security.Claims;
using CatalogSync.Application.DTOs.Libros;
using CatalogSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CatalogSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibrosController(ILibroService libros, IWebHostEnvironment env) : ControllerBase
{
    // Magic bytes for allowed image formats
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { ".jpg",  [0xFF, 0xD8, 0xFF] },
        { ".jpeg", [0xFF, 0xD8, 0xFF] },
        { ".png",  [0x89, 0x50, 0x4E, 0x47] },
        { ".webp", [0x52, 0x49, 0x46, 0x46] }
    };

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("publica")]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool soloConExistencia = false,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool sinExistencia = false)
    {
        var result = await libros.BuscarAsync(q, page, pageSize, soloConExistencia, isActive, sinExistencia);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("publica")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var libro = await libros.GetByIdAsync(id);
        return libro is null ? NotFound() : Ok(libro);
    }

    [HttpGet("isbn/{isbn}")]
    [AllowAnonymous]
    [EnableRateLimiting("publica")]
    public async Task<IActionResult> GetByIsbn(string isbn)
    {
        var libro = await libros.GetByIsbnAsync(isbn);
        return libro is null ? NotFound() : Ok(libro);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear([FromBody] CrearLibroRequest request)
    {
        try
        {
            var adminId = GetUserId();
            var libro = await libros.CrearAsync(request, adminId);
            return CreatedAtAction(nameof(GetById), new { id = libro.Id }, libro);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarLibroRequest request)
    {
        try
        {
            var libro = await libros.ActualizarAsync(id, request);
            return Ok(libro);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/precio")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActualizarPrecio(Guid id, [FromBody] ActualizarPrecioRequest request)
    {
        try
        {
            await libros.ActualizarPrecioAsync(id, request, GetUserId());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/inventario")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActualizarInventario(Guid id, [FromBody] ActualizarInventarioRequest request)
    {
        try
        {
            await libros.ActualizarInventarioAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/ubicacion")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActualizarUbicacion(Guid id, [FromBody] ActualizarUbicacionRequest request)
    {
        try
        {
            await libros.ActualizarUbicacionAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/desactivar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        try
        {
            await libros.DesactivarAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activar(Guid id)
    {
        try
        {
            await libros.ActivarAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("portadas/bulk")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("bulk")]
    public async Task<IActionResult> SubirPortadasBulk(IList<IFormFile> archivos)
    {
        if (archivos is null || archivos.Count == 0)
            return BadRequest(new { message = "No se enviaron archivos." });

        if (archivos.Count > 50)
            return BadRequest(new { message = "Máximo 50 imágenes por carga." });

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder  = Path.Combine(webRoot, "uploads", "portadas");
        Directory.CreateDirectory(folder);

        var portadas = new List<(string Isbn, string Url, string Archivo)>();
        var erroresFormato = new List<BulkPortadasItem>();

        foreach (var archivo in archivos)
        {
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            {
                erroresFormato.Add(new BulkPortadasItem(archivo.FileName, "", null, "ExtensionNoValida"));
                continue;
            }
            if (archivo.Length > 8 * 1024 * 1024)
            {
                erroresFormato.Add(new BulkPortadasItem(archivo.FileName, "", null, "DemasiadoGrande"));
                continue;
            }
            if (!await EsImagenValidaAsync(archivo, ext))
            {
                erroresFormato.Add(new BulkPortadasItem(archivo.FileName, "", null, "ExtensionNoValida"));
                continue;
            }

            var isbn     = Path.GetFileNameWithoutExtension(archivo.FileName).Trim();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = System.IO.File.Create(filePath))
                await archivo.CopyToAsync(stream);

            var url = $"{Request.Scheme}://{Request.Host}/uploads/portadas/{fileName}";
            portadas.Add((isbn, url, archivo.FileName));
        }

        var result = await libros.BulkPortadasAsync(portadas);

        return Ok(new BulkPortadasResult(
            result.Asignadas,
            result.NoEncontradas,
            result.Errores + erroresFormato.Count,
            [.. result.Detalles, .. erroresFormato]
        ));
    }

    [HttpPost("{id:guid}/portada")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SubirPortada(Guid id, IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Archivo vacío." });
        if (archivo.Length > 8 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede superar 8 MB." });

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG o WEBP." });

        if (!await EsImagenValidaAsync(archivo, ext))
            return BadRequest(new { message = "El contenido del archivo no corresponde a una imagen válida." });

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder  = Path.Combine(webRoot, "uploads", "portadas");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, fileName);

        using (var stream = System.IO.File.Create(filePath))
            await archivo.CopyToAsync(stream);

        var url = $"{Request.Scheme}://{Request.Host}/uploads/portadas/{fileName}";
        await libros.ActualizarPortadaAsync(id, url);
        return Ok(new { url });
    }

    [HttpPost("bulk/accion")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("bulk")]
    public async Task<IActionResult> BulkAccion([FromBody] BulkLibrosRequest request)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest(new { message = "Debe seleccionar al menos un libro." });
        if (request.Accion != "activar" && request.Accion != "desactivar")
            return BadRequest(new { message = "Acción no válida." });

        var result = await libros.BulkAccionAsync(request.Ids, request.Accion);
        return Ok(result);
    }

    [HttpPost("bulk/desactivar-agotados")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("bulk")]
    public async Task<IActionResult> DesactivarAgotados()
    {
        var result = await libros.DesactivarAgotadosAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}/historial-precios")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HistorialPrecios(Guid id)
    {
        var historial = await libros.GetHistorialPreciosAsync(id);
        return Ok(historial);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<bool> EsImagenValidaAsync(IFormFile archivo, string ext)
    {
        if (!MagicBytes.TryGetValue(ext, out var magic)) return false;
        var buffer = new byte[magic.Length];
        using var stream = archivo.OpenReadStream();
        var leidos = await stream.ReadAsync(buffer);
        return leidos == magic.Length && buffer.SequenceEqual(magic);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.Parse(sub!);
    }
}
