using System.Security.Claims;
using LibreriaAtenas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibreriaAtenas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionesController(INotificacionService notificaciones) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMisSolicitudes()
    {
        var result = await notificaciones.GetMisSolicitudesAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("{libroId:guid}")]
    public async Task<IActionResult> Solicitar(Guid libroId)
    {
        try
        {
            var id = await notificaciones.SolicitarAsync(GetUserId(), libroId);
            return Created($"/api/notificaciones/{id}", new { id });
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        try
        {
            await notificaciones.CancelarAsync(id, GetUserId());
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.Parse(sub!);
    }
}
