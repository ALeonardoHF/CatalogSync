using CatalogSync.Api.Extensions;
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
        var result = await favoritos.GetMisFavoritosAsync(User.GetUserId());
        return Ok(result);
    }

    [HttpPost("{libroId:guid}")]
    public async Task<IActionResult> Agregar(Guid libroId)
    {
        try
        {
            var id = await favoritos.AgregarAsync(User.GetUserId(), libroId);
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
            await favoritos.QuitarAsync(User.GetUserId(), libroId);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
