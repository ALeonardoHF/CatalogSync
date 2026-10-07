using System.Security.Claims;
using CatalogSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritosController(IFavoritoService favoritos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMisFavoritos()
    {
        var result = await favoritos.GetMisFavoritosAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("{libroId:guid}")]
    public async Task<IActionResult> Agregar(Guid libroId)
    {
        try
        {
            var id = await favoritos.AgregarAsync(GetUserId(), libroId);
            return Created($"/api/favoritos/{id}", new { id });
        }
        catch (KeyNotFoundException ex)    { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{libroId:guid}")]
    public async Task<IActionResult> Quitar(Guid libroId)
    {
        try
        {
            await favoritos.QuitarAsync(GetUserId(), libroId);
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
