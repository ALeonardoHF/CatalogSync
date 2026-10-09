using CatalogSync.Api.Extensions;
using CatalogSync.Application.DTOs.Auth;
using CatalogSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? rol)
    {
        var result = await usuarios.ListarAsync(rol);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AdminCrearUsuarioRequest request)
    {
        try
        {
            var usuario = await usuarios.CrearAsync(request);
            return Ok(usuario);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPatch("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id)
    {
        try
        {
            await usuarios.ActivarAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var myId = User.GetUserId();

        try
        {
            await usuarios.DesactivarAsync(id, myId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException)         { return NotFound(); }
    }
}
