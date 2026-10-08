using CatalogSync.Application.DTOs.Auth;
using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Infrastructure.Services;
using CatalogSync.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CatalogSync.Tests.Services;

public class AuthServiceTests
{
    // BCrypt cost 4 to keep tests fast
    private const string TestPassword = "P@ssw0rd123";
    private static readonly string TestPasswordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword, 4);

    private static LibreriaDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<LibreriaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]                  = "test-secret-key-must-be-at-least-32-chars!!",
                ["JwtSettings:Issuer"]                     = "TestIssuer",
                ["JwtSettings:Audience"]                   = "TestAudience",
                ["JwtSettings:ExpirationMinutes"]          = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

    private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    private static AuthService CreateService(LibreriaDbContext db, IMemoryCache? cache = null) =>
        new(db, CreateConfig(), cache ?? CreateCache());

    private static async Task<Usuario> SeedUsuario(
        LibreriaDbContext db,
        Role role = Role.Cliente,
        bool activo = true,
        int intentosFallidos = 0)
    {
        var usuario = Usuario.Create("test@test.com", TestPasswordHash, "Test User", role);
        if (!activo) usuario.Desactivar();
        for (int i = 0; i < intentosFallidos; i++) usuario.RegistrarLoginFallido();
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsUnauthorized()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest("nobody@test.com", TestPassword)));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorized()
    {
        using var db = CreateDb();
        await SeedUsuario(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest("test@test.com", "WrongPassword!")));
    }

    [Fact]
    public async Task LoginAsync_WithInactiveAccount_ThrowsUnauthorized()
    {
        using var db = CreateDb();
        await SeedUsuario(db, activo: false);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest("test@test.com", TestPassword)));
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ThrowsUnauthorized()
    {
        using var db = CreateDb();
        await SeedUsuario(db, intentosFallidos: 5);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest("test@test.com", TestPassword)));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        using var db = CreateDb();
        await SeedUsuario(db);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("test@test.com", result.Email);
        Assert.Equal(Role.Cliente.ToString(), result.Role);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_CreatesRefreshTokenInDb()
    {
        using var db = CreateDb();
        var usuario = await SeedUsuario(db);
        var svc = CreateService(db);

        await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));

        var tokens = await db.RefreshTokens.Where(t => t.UsuarioId == usuario.Id).ToListAsync();
        Assert.Single(tokens);
        Assert.False(tokens[0].Revocado);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_IncrementsFailedAttempts()
    {
        using var db = CreateDb();
        var usuario = await SeedUsuario(db);
        var svc = CreateService(db);

        try { await svc.LoginAsync(new LoginRequest("test@test.com", "wrong")); }
        catch (UnauthorizedAccessException) { }

        Assert.Equal(1, usuario.IntentosFallidos);
    }

    [Fact]
    public async Task LoginAsync_After5FailedAttempts_BlocksAccount()
    {
        using var db = CreateDb();
        var usuario = await SeedUsuario(db);
        var svc = CreateService(db);

        for (int i = 0; i < 5; i++)
        {
            try { await svc.LoginAsync(new LoginRequest("test@test.com", "wrong")); }
            catch (UnauthorizedAccessException) { }
        }

        Assert.True(usuario.EstaBloqueado());
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_SetsTokenVersionInCache()
    {
        using var db = CreateDb();
        await SeedUsuario(db);
        var cache = CreateCache();
        var svc = CreateService(db, cache);

        var result = await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));

        var usuario = await db.Usuarios.FirstAsync(u => u.Email == "test@test.com");
        var inCache = cache.TryGetValue($"tv_{usuario.Id}", out int cachedVersion);
        Assert.True(inCache);
        Assert.Equal(usuario.TokenVersion, cachedVersion);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperation()
    {
        using var db = CreateDb();
        await SeedUsuario(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RegisterAsync(new RegisterRequest("test@test.com", "NewPass123!", "Nuevo")));
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsAuthResponse()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.RegisterAsync(
            new RegisterRequest("new@test.com", "P@ssw0rd123", "New User"));

        Assert.NotNull(result);
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("Cliente", result.Role);
    }

    [Fact]
    public async Task RegisterAsync_AlwaysAssignsClienteRole()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.RegisterAsync(
            new RegisterRequest("admin@test.com", "P@ssw0rd123", "Fake Admin"));

        Assert.Equal("Cliente", result.Role);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailToLowercase()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        var result = await svc.RegisterAsync(
            new RegisterRequest("User@TEST.com", "P@ssw0rd123", "User"));

        Assert.Equal("user@test.com", result.Email);
    }

    // ── LogoutAllAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAllAsync_IncrementsTokenVersion()
    {
        using var db = CreateDb();
        var usuario = await SeedUsuario(db);
        var versionInicial = usuario.TokenVersion;
        var svc = CreateService(db);

        await svc.LogoutAllAsync(usuario.Id);

        await db.Entry(usuario).ReloadAsync();
        Assert.Equal(versionInicial + 1, usuario.TokenVersion);
    }

    [Fact]
    public async Task LogoutAllAsync_RevokesAllActiveRefreshTokens()
    {
        using var db = CreateDb();
        await SeedUsuario(db);
        var svc = CreateService(db);

        // Generate two sessions
        var res1 = await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));
        var res2 = await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));

        var usuario = await db.Usuarios.FirstAsync(u => u.Email == "test@test.com");
        await svc.LogoutAllAsync(usuario.Id);

        var tokens = await db.RefreshTokens.Where(t => t.UsuarioId == usuario.Id).ToListAsync();
        Assert.All(tokens, t => Assert.True(t.Revocado));
        _ = res1; _ = res2; // used for side effects
    }

    [Fact]
    public async Task LogoutAllAsync_RemovesTokenVersionFromCache()
    {
        using var db = CreateDb();
        var usuario = await SeedUsuario(db);
        var cache = CreateCache();
        var svc = CreateService(db, cache);

        cache.Set($"tv_{usuario.Id}", usuario.TokenVersion, TimeSpan.FromSeconds(30));

        await svc.LogoutAllAsync(usuario.Id);

        Assert.False(cache.TryGetValue($"tv_{usuario.Id}", out _));
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WithValidToken_RevokesIt()
    {
        using var db = CreateDb();
        await SeedUsuario(db);
        var svc = CreateService(db);

        var loginResult = await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));

        await svc.LogoutAsync(loginResult.RefreshToken);

        var token = await db.RefreshTokens.FirstAsync(t => t.Token == loginResult.RefreshToken);
        Assert.True(token.Revocado);
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownToken_DoesNotThrow()
    {
        using var db = CreateDb();
        var svc = CreateService(db);

        // Should not throw when token doesn't exist
        await svc.LogoutAsync("nonexistent-token");
    }

    // ── Contenido del token ──────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_TokenIncludesNombreCompleto()
    {
        // Protege el fix de /api/auth/me: el nombre del usuario debe viajar
        // en el token para que Me() pueda leerlo via User.Identity.Name.
        using var db = CreateDb();
        await SeedUsuario(db);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(new LoginRequest("test@test.com", TestPassword));

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);

        Assert.Contains(jwt.Claims, c => c.Value == "Test User");
    }
}
