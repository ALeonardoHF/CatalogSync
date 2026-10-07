using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogSync.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<LibreriaDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("Default"),
                sql => sql.MigrationsAssembly(typeof(LibreriaDbContext).Assembly.FullName)));

        return services;
    }
}
