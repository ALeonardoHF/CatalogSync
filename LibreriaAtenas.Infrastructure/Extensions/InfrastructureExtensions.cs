using LibreriaAtenas.Application.Interfaces;
using LibreriaAtenas.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibreriaAtenas.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IArchivoTemporalService, ArchivoTemporalService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILibroService, LibroService>();
        services.AddScoped<IFavoritoService, FavoritoService>();
        services.AddScoped<INotificacionService, NotificacionService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        return services;
    }
}
