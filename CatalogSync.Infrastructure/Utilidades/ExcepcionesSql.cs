using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CatalogSync.Infrastructure.Utilidades;

public static class ExcepcionesSql
{
    /// <summary>
    /// Todo "verificar que no exista y luego insertar" (favoritos, registro
    /// de usuario, alta de libro por ISBN, solicitudes de notificación)
    /// tiene la misma condición de carrera real: dos peticiones pueden
    /// pasar el chequeo antes de que ninguna se haya guardado todavía. El
    /// índice único en la base de datos es la última línea de defensa, y
    /// esto distingue esa violación de cualquier otro error de base de
    /// datos.
    /// </summary>
    public static bool EsViolacionDeUnicidad(this DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
