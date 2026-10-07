using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Domain.Exceptions;

namespace CatalogSync.Tests.Domain;

public class UsuarioDomainTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        var usuario = Usuario.Create("user@test.com", "hash123", "Juan Perez", Role.Cliente);

        Assert.Equal("user@test.com", usuario.Email);
        Assert.Equal("hash123", usuario.PasswordHash);
        Assert.Equal("Juan Perez", usuario.NombreCompleto);
        Assert.Equal(Role.Cliente, usuario.Role);
        Assert.True(usuario.IsActive);
        Assert.Equal(1, usuario.TokenVersion);
        Assert.NotEqual(Guid.Empty, usuario.Id);
    }

    [Fact]
    public void Create_NormalizesEmailToLowercase()
    {
        var usuario = Usuario.Create("USER@TEST.COM", "hash", "Test", Role.Admin);

        Assert.Equal("user@test.com", usuario.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ThrowsDomainException(string email)
    {
        Assert.Throws<DomainException>(() => Usuario.Create(email, "hash", "Test", Role.Cliente));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPasswordHash_ThrowsDomainException(string hash)
    {
        Assert.Throws<DomainException>(() => Usuario.Create("test@test.com", hash, "Test", Role.Cliente));
    }

    // ── Bloqueo por intentos fallidos ─────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RegistrarLoginFallido_Before5Attempts_DoesNotBlock(int intentos)
    {
        var usuario = CrearUsuario();
        for (int i = 0; i < intentos; i++) usuario.RegistrarLoginFallido();

        Assert.False(usuario.EstaBloqueado());
        Assert.Null(usuario.BloqueadoHasta);
    }

    [Fact]
    public void RegistrarLoginFallido_At5thAttempt_BlocksAccount()
    {
        var usuario = CrearUsuario();
        for (int i = 0; i < 5; i++) usuario.RegistrarLoginFallido();

        Assert.True(usuario.EstaBloqueado());
        Assert.NotNull(usuario.BloqueadoHasta);
    }

    [Fact]
    public void RegistrarLoginFallido_BlocksFor15Minutes()
    {
        var antes = DateTime.UtcNow;
        var usuario = CrearUsuario();
        for (int i = 0; i < 5; i++) usuario.RegistrarLoginFallido();

        Assert.True(usuario.BloqueadoHasta >= antes.AddMinutes(14));
        Assert.True(usuario.BloqueadoHasta <= antes.AddMinutes(16));
    }

    [Fact]
    public void EstaBloqueado_WhenNeverBlocked_ReturnsFalse()
    {
        Assert.False(CrearUsuario().EstaBloqueado());
    }

    [Fact]
    public void EstaBloqueado_WhenBloqueadoHastaIsInFuture_ReturnsTrue()
    {
        var usuario = CrearUsuario();
        for (int i = 0; i < 5; i++) usuario.RegistrarLoginFallido();

        Assert.True(usuario.EstaBloqueado());
    }

    // ── RegistrarLoginExitoso ─────────────────────────────────────────────────

    [Fact]
    public void RegistrarLoginExitoso_ResetsFallidosAndClearsBloqueo()
    {
        var usuario = CrearUsuario();
        for (int i = 0; i < 5; i++) usuario.RegistrarLoginFallido();

        usuario.RegistrarLoginExitoso();

        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Null(usuario.BloqueadoHasta);
        Assert.False(usuario.EstaBloqueado());
        Assert.NotNull(usuario.UltimoLogin);
    }

    // ── TokenVersion ──────────────────────────────────────────────────────────

    [Fact]
    public void IncrementarTokenVersion_IncreasesVersionByOne()
    {
        var usuario = CrearUsuario();
        var version = usuario.TokenVersion;

        usuario.IncrementarTokenVersion();

        Assert.Equal(version + 1, usuario.TokenVersion);
    }

    [Fact]
    public void ActualizarPassword_IncreasesTokenVersionAndUpdatesHash()
    {
        var usuario = CrearUsuario();
        var version = usuario.TokenVersion;

        usuario.ActualizarPassword("newHash");

        Assert.Equal(version + 1, usuario.TokenVersion);
        Assert.Equal("newHash", usuario.PasswordHash);
    }

    // ── Activar / Desactivar ──────────────────────────────────────────────────

    [Fact]
    public void Desactivar_SetsIsActiveToFalse()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();

        Assert.False(usuario.IsActive);
    }

    [Fact]
    public void Activar_SetsIsActiveToTrue()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();
        usuario.Activar();

        Assert.True(usuario.IsActive);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Usuario CrearUsuario() =>
        Usuario.Create("test@test.com", "hash123", "Test User", Role.Cliente);
}
