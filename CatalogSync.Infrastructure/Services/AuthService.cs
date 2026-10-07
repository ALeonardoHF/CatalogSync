using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CatalogSync.Application.DTOs.Auth;
using CatalogSync.Application.Interfaces;
using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CatalogSync.Infrastructure.Services;

public class AuthService(LibreriaDbContext db, IConfiguration config, IMemoryCache cache) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.ToLowerInvariant().Trim();

        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("El email ya está registrado.");

        var hash    = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var usuario = Usuario.Create(email, hash, request.NombreCompleto, Role.Cliente);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return await GenerarTokensAsync(usuario);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var email   = request.Email.ToLowerInvariant().Trim();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!usuario.IsActive)
            throw new UnauthorizedAccessException("Cuenta desactivada.");

        if (usuario.EstaBloqueado())
            throw new UnauthorizedAccessException("Cuenta bloqueada temporalmente. Intenta en unos minutos.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            usuario.RegistrarLoginFallido();
            await db.SaveChangesAsync();
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        usuario.RegistrarLoginExitoso();
        await db.SaveChangesAsync();

        return await GenerarTokensAsync(usuario, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress = null)
    {
        var token = await db.RefreshTokens
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.Token == refreshToken)
            ?? throw new UnauthorizedAccessException("Refresh token inválido.");

        if (!token.EsValido())
            throw new UnauthorizedAccessException("Refresh token expirado o revocado.");

        if (!token.Usuario.IsActive)
            throw new UnauthorizedAccessException("Cuenta desactivada.");

        token.Revocar();
        await db.SaveChangesAsync();

        return await GenerarTokensAsync(token.Usuario, ipAddress);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token is not null)
        {
            token.Revocar();
            await db.SaveChangesAsync();
        }
    }

    public async Task LogoutAllAsync(Guid usuarioId)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && !t.Revocado)
            .ToListAsync();

        foreach (var t in tokens) t.Revocar();

        var usuario = await db.Usuarios.FindAsync(usuarioId);
        usuario?.IncrementarTokenVersion();

        await db.SaveChangesAsync();

        cache.Remove($"tv_{usuarioId}");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AuthResponse> GenerarTokensAsync(Usuario usuario, string? ip = null)
    {
        var jwt        = config.GetSection("JwtSettings");
        var expMinutos = int.Parse(jwt["ExpirationMinutes"] ?? "60");
        var expDias    = int.Parse(jwt["RefreshTokenExpirationDays"] ?? "7");

        var expiresAt       = DateTime.UtcNow.AddMinutes(expMinutos);
        var accessToken     = GenerarJwt(usuario, expiresAt, jwt);
        var refreshTokenStr = GenerarRefreshToken();

        var rt = RefreshToken.Create(usuario.Id, refreshTokenStr, expDias, ip);
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();

        cache.Set($"tv_{usuario.Id}", usuario.TokenVersion, TimeSpan.FromSeconds(30));

        return new AuthResponse(
            accessToken,
            refreshTokenStr,
            expiresAt,
            usuario.NombreCompleto,
            usuario.Email,
            usuario.Role.ToString());
    }

    private string GenerarJwt(Usuario usuario, DateTime expiresAt, IConfigurationSection jwt)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Role,               usuario.Role.ToString()),
            new Claim("tokenVersion",                usuario.TokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer:             jwt["Issuer"],
            audience:           jwt["Audience"],
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private static string GenerarRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
