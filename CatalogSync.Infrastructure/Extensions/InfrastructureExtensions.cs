using CatalogSync.Application.Interfaces;
using CatalogSync.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogSync.Infrastructure.Extensions;

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
