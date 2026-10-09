using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CatalogSync.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// El id del usuario autenticado. JwtSecurityTokenHandler remapea
    /// "sub" a ClaimTypes.NameIdentifier al validar el token (ver el
    /// OnTokenValidated en Program.cs) — por eso se busca primero el
    /// claim largo, con el corto como respaldo.
    ///
    /// Antes esto estaba copiado y pegado en 6 controllers distintos,
    /// con dos estilos diferentes (uno lanzaba si el claim no era un
    /// Guid valido, otro devolvia Guid.Empty) — exactamente el tipo de
    /// duplicacion que hizo que una de las seis copias (AuthController.Me)
    /// se desincronizara del resto y quedara rota.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
