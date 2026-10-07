using LibreriaAtenas.Application.DTOs.Auth;
using LibreriaAtenas.Application.DTOs.Usuarios;
using LibreriaAtenas.Application.Interfaces;
using LibreriaAtenas.Domain.Entities;
using LibreriaAtenas.Domain.Enums;
using LibreriaAtenas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibreriaAtenas.Infrastructure.Services;

public class UsuarioService(LibreriaDbContext db) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(string? rol)
    {
        var query = db.Usuarios.AsQueryable();

        if (Enum.TryParse<Role>(rol, ignoreCase: true, out var roleEnum))
            query = query.Where(u => u.Role == roleEnum);

        return await query
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new UsuarioDto(
                u.Id, u.Email, u.NombreCompleto,
                u.Role.ToString(), u.IsActive, u.CreadoEn, u.UltimoLogin))
            .ToListAsync();
    }

    public async Task<UsuarioDto> CrearAsync(AdminCrearUsuarioRequest request)
    {
        var email = request.Email.ToLowerInvariant().Trim();

        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("El email ya está registrado.");

        var hash    = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var usuario = Usuario.Create(email, hash, request.NombreCompleto.Trim(), request.Role);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return new UsuarioDto(
            usuario.Id, usuario.Email, usuario.NombreCompleto,
            usuario.Role.ToString(), usuario.IsActive, usuario.CreadoEn, null);
    }

    public async Task ActivarAsync(Guid id)
    {
        var usuario = await db.Usuarios.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        usuario.Activar();
        await db.SaveChangesAsync();
    }

    public async Task DesactivarAsync(Guid id, Guid solicitanteId)
    {
        if (id == solicitanteId)
            throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");

        var usuario = await db.Usuarios.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        usuario.Desactivar();
        await db.SaveChangesAsync();
    }
}
