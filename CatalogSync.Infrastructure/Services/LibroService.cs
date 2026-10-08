using CatalogSync.Application.DTOs.Libros;
using CatalogSync.Application.Interfaces;
using CatalogSync.Domain.Entities;
using CatalogSync.Domain.Enums;
using CatalogSync.Domain.Servicios;
using CatalogSync.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CatalogSync.Infrastructure.Services;

public class LibroService(LibreriaDbContext db) : ILibroService
{
    public async Task<PagedResult<LibroDto>> BuscarAsync(string? q, int page, int pageSize, bool soloConExistencia = false, bool? isActive = null, bool sinExistencia = false)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var query = db.Libros
            .Include(l => l.Inventario)
            .Include(l => l.Ubicacion)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(l => l.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            // Sin .ToLower(): SQL Server ya compara LIKE sin distinguir
            // mayúsculas con su collation por default (_CI_). Envolver la
            // columna en LOWER() obliga a evaluarla fila por fila en cada
            // busqueda sobre ~56,000 libros, y le quita a SQL Server la
            // posibilidad de usar un indice sobre estas columnas si se
            // agrega uno mas adelante.
            var term = q.Trim();
            query = query.Where(l =>
                l.ISBN.Contains(term) ||
                l.Titulo.Contains(term) ||
                l.Autor.Contains(term) ||
                l.Editorial.Contains(term));
        }

        if (soloConExistencia)
            query = query.Where(l => l.Inventario != null && l.Inventario.Existencia > 0);

        if (sinExistencia)
            query = query.Where(l => l.Inventario == null || l.Inventario.Existencia == 0);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.Titulo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<LibroDto>(items.Select(l => ToDto(l)).ToList(), total, page, pageSize);
    }

    public async Task<LibroDto?> GetByIdAsync(Guid id)
    {
        var libro = await db.Libros
            .Include(l => l.Inventario)
            .Include(l => l.Ubicacion)
            .FirstOrDefaultAsync(l => l.Id == id);
        return libro is null ? null : ToDto(libro);
    }

    public async Task<LibroDto?> GetByIsbnAsync(string isbn)
    {
        var libro = await db.Libros
            .Include(l => l.Inventario)
            .Include(l => l.Ubicacion)
            .FirstOrDefaultAsync(l => l.ISBN == isbn.Trim());
        return libro is null ? null : ToDto(libro);
    }

    public async Task<LibroDto> CrearAsync(CrearLibroRequest request, Guid adminId)
    {
        if (await db.Libros.AnyAsync(l => l.ISBN == request.ISBN.Trim()))
            throw new InvalidOperationException($"Ya existe un libro con ISBN {request.ISBN}.");

        var libro = Libro.Create(request.ISBN, request.Titulo, request.Autor,
            request.Editorial, request.PrecioVenta, request.Costo);

        libro.ActualizarMetadatos(request.Titulo, request.Autor, request.Editorial,
            request.Costo, request.Descuento);

        libro.ActualizarDatosAdicionales(request.Portada, request.Sinopsis, request.Genero,
            request.Paginas, request.AnioPublicacion, request.CodigoBarra);

        var inventario = Inventario.Create(libro.Id, 0);
        db.Libros.Add(libro);
        db.Inventarios.Add(inventario);
        await db.SaveChangesAsync();

        return ToDto(libro, inventario);
    }

    public async Task<LibroDto> ActualizarAsync(Guid id, ActualizarLibroRequest request)
    {
        var libro = await db.Libros.Include(l => l.Inventario).Include(l => l.Ubicacion)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException("Libro no encontrado.");

        libro.ActualizarMetadatos(request.Titulo, request.Autor, request.Editorial,
            request.Costo, request.Descuento);

        libro.ActualizarDatosAdicionales(request.Portada, request.Sinopsis, request.Genero,
            request.Paginas, request.AnioPublicacion, request.CodigoBarra);

        await db.SaveChangesAsync();
        return ToDto(libro);
    }

    public async Task ActualizarPrecioAsync(Guid id, ActualizarPrecioRequest request, Guid adminId)
    {
        var libro = await db.Libros.FindAsync(id)
            ?? throw new KeyNotFoundException("Libro no encontrado.");

        var historial = HistorialPrecio.Create(libro.Id, libro.PrecioVenta, request.PrecioVenta,
            request.Fuente ?? "Manual", adminId);

        libro.ActualizarPrecio(request.PrecioVenta);

        if (request.Costo.HasValue)
            libro.ActualizarMetadatos(libro.Titulo, libro.Autor, libro.Editorial,
                request.Costo.Value, libro.Descuento);

        db.HistorialPrecios.Add(historial);
        await db.SaveChangesAsync();
    }

    public async Task ActualizarInventarioAsync(Guid id, ActualizarInventarioRequest request)
    {
        var inventario = await db.Inventarios.FirstOrDefaultAsync(i => i.LibroId == id)
            ?? throw new KeyNotFoundException("Inventario no encontrado.");

        inventario.ActualizarExistencia(request.Existencia);
        await db.SaveChangesAsync();
    }

    public async Task ActualizarUbicacionAsync(Guid id, ActualizarUbicacionRequest request)
    {
        var ubicacion = await db.Ubicaciones.FirstOrDefaultAsync(u => u.LibroId == id);

        if (ubicacion is null)
        {
            ubicacion = Ubicacion.Create(id, request.Tipo, request.Seccion,
                request.Estante, request.Referencia, request.Notas);
            db.Ubicaciones.Add(ubicacion);
        }
        else
        {
            ubicacion.Actualizar(request.Tipo, request.Seccion, request.Estante,
                request.Referencia, request.Notas);
        }

        await db.SaveChangesAsync();
    }

    public async Task DesactivarAsync(Guid id)
    {
        var libro = await db.Libros.FindAsync(id)
            ?? throw new KeyNotFoundException("Libro no encontrado.");
        libro.Desactivar();
        await db.SaveChangesAsync();
    }

    public async Task ActivarAsync(Guid id)
    {
        var libro = await db.Libros.FindAsync(id)
            ?? throw new KeyNotFoundException("Libro no encontrado.");
        libro.Activar();
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<HistorialPrecioDto>> GetHistorialPreciosAsync(Guid libroId)
    {
        var historial = await db.HistorialPrecios
            .Where(h => h.LibroId == libroId)
            .OrderByDescending(h => h.CambiadoEn)
            .ToListAsync();

        return historial.Select(h => new HistorialPrecioDto(
            h.Id, h.PrecioAnterior, h.PrecioNuevo, h.Fuente, h.CambiadoPorId, h.CambiadoEn
        )).ToList();
    }

    public async Task<ImportarCatalogoResult> ImportarCatalogoAsync(IEnumerable<LibroImportItem> items, Guid adminId, EstrategiaPrecio estrategia = EstrategiaPrecio.MasAltoSiHayExistencia)
    {
        var itemList = items.Where(i => !string.IsNullOrWhiteSpace(i.ISBN)).ToList();
        var isbns    = itemList.Select(i => i.ISBN).ToHashSet();

        var existentes = await db.Libros
            .Include(l => l.Inventario)
            .Where(l => isbns.Contains(l.ISBN))
            .ToDictionaryAsync(l => l.ISBN);

        int creados = 0, actualizados = 0, sinCambio = 0, errores = 0, revisar = 0;
        var mensajes = new List<string>();
        var mensajesRevisar = new List<string>();
        var historialesNuevos = new List<HistorialPrecio>();
        var inventariosNuevos = new List<Inventario>();

        foreach (var item in itemList)
        {
            try
            {
                if (existentes.TryGetValue(item.ISBN, out var libro))
                {
                    bool cambio = false;

                    // El precio se decide contra el estado VIVO de la base
                    // de datos, no contra lo que diga el Excel — si alguien
                    // ya ajusto el precio manualmente despues de exportar
                    // el catalogo, no se pierde por reimportar un archivo
                    // desactualizado. Misma regla que en CatalogoService,
                    // para que procesar, importar-bd e importar-archivo
                    // se comporten igual.
                    var existenciaActual = libro.Inventario?.Existencia ?? 0;
                    var precioResuelto = estrategia.ResolverPrecio(libro.PrecioVenta, item.PrecioVenta, existenciaActual);

                    if (precioResuelto != libro.PrecioVenta)
                    {
                        historialesNuevos.Add(HistorialPrecio.Create(
                            libro.Id, libro.PrecioVenta, precioResuelto, "ImportExcel", adminId));
                        libro.ActualizarPrecio(precioResuelto);
                        cambio = true;
                    }

                    if (item.Existencia >= 0 && libro.Inventario is not null &&
                        libro.Inventario.Existencia != item.Existencia)
                    {
                        libro.Inventario.ActualizarExistencia(item.Existencia);
                        cambio = true;
                    }

                    // Completar metadatos vacios, nunca sobrescribir los
                    // que ya existen — las diferencias reales se reportan
                    // para revision manual en vez de aplicarse solas.
                    var titulo = libro.Titulo;
                    var autor = libro.Autor;
                    var editorial = libro.Editorial;
                    var metadatosCompletados = false;
                    var diferencias = new List<string>();

                    RevisionDeCampos.Revisar(diferencias, "Título", libro.Titulo, item.Titulo, v => { titulo = v; metadatosCompletados = true; });
                    RevisionDeCampos.Revisar(diferencias, "Autor", libro.Autor, item.Autor, v => { autor = v; metadatosCompletados = true; });
                    RevisionDeCampos.Revisar(diferencias, "Editorial", libro.Editorial, item.Editorial, v => { editorial = v; metadatosCompletados = true; });

                    if (metadatosCompletados)
                    {
                        libro.ActualizarMetadatos(titulo, autor, editorial, libro.Costo, libro.Descuento);
                        cambio = true;
                    }

                    if (diferencias.Count > 0)
                    {
                        revisar++;
                        mensajesRevisar.Add($"ISBN {item.ISBN}: {string.Join("; ", diferencias)}");
                    }

                    if (cambio) actualizados++;
                    else sinCambio++;
                }
                else
                {
                    var nuevo = Libro.Create(item.ISBN, item.Titulo, item.Autor,
                        item.Editorial, item.PrecioVenta, item.Costo);

                    nuevo.ActualizarDatosAdicionales(null, null, null, null, null, item.CodigoBarra);
                    db.Libros.Add(nuevo);

                    var inv = Inventario.Create(nuevo.Id, item.Existencia);
                    inventariosNuevos.Add(inv);
                    existentes[item.ISBN] = nuevo;

                    creados++;
                }
            }
            catch (Exception ex)
            {
                errores++;
                mensajes.Add($"ISBN {item.ISBN}: {ex.Message}");
            }
        }

        db.Inventarios.AddRange(inventariosNuevos);
        db.HistorialPrecios.AddRange(historialesNuevos);
        await db.SaveChangesAsync();

        return new ImportarCatalogoResult(creados, actualizados, sinCambio, errores, mensajes, revisar, mensajesRevisar);
    }

    public async Task<BulkLibrosResult> BulkAccionAsync(List<Guid> ids, string accion)
    {
        var libros = await db.Libros.Where(l => ids.Contains(l.Id)).ToListAsync();

        int afectados = 0, errores = 0;
        foreach (var libro in libros)
        {
            try
            {
                if (accion == "activar") libro.Activar();
                else                     libro.Desactivar();
                afectados++;
            }
            catch { errores++; }
        }

        await db.SaveChangesAsync();
        return new BulkLibrosResult(afectados, errores);
    }

    public async Task<BulkPortadasResult> BulkPortadasAsync(IReadOnlyList<(string Isbn, string Url, string Archivo)> portadas)
    {
        var isbns = portadas.Select(p => p.Isbn).ToHashSet();
        var mapa  = await db.Libros.Where(l => isbns.Contains(l.ISBN)).ToDictionaryAsync(l => l.ISBN);

        int asignadas = 0, noEncontradas = 0;
        var detalles = new List<BulkPortadasItem>();

        foreach (var (isbn, url, archivo) in portadas)
        {
            if (mapa.TryGetValue(isbn, out var libro))
            {
                libro.ActualizarPortada(url);
                asignadas++;
                detalles.Add(new BulkPortadasItem(archivo, isbn, libro.Titulo, "OK"));
            }
            else
            {
                noEncontradas++;
                detalles.Add(new BulkPortadasItem(archivo, isbn, null, "NoEncontrado"));
            }
        }

        await db.SaveChangesAsync();
        return new BulkPortadasResult(asignadas, noEncontradas, 0, detalles);
    }

    public async Task<string> ActualizarPortadaAsync(Guid id, string url)
    {
        var libro = await db.Libros.FindAsync(id)
            ?? throw new KeyNotFoundException("Libro no encontrado.");
        libro.ActualizarPortada(url);
        await db.SaveChangesAsync();
        return url;
    }

    public async Task<BulkLibrosResult> DesactivarAgotadosAsync()
    {
        var libros = await db.Libros
            .Include(l => l.Inventario)
            .Where(l => l.IsActive && (l.Inventario == null || l.Inventario.Existencia == 0))
            .ToListAsync();

        foreach (var libro in libros) libro.Desactivar();

        await db.SaveChangesAsync();
        return new BulkLibrosResult(libros.Count, 0);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static LibroDto ToDto(Libro l, Inventario? inv = null)
    {
        var inventario = inv ?? l.Inventario;
        return new LibroDto(
            l.Id, l.ISBN, l.Titulo, l.Autor, l.Editorial,
            l.PrecioVenta, l.Costo, l.Descuento,
            l.Portada, l.Sinopsis, l.Genero, l.Paginas, l.AnioPublicacion, l.CodigoBarra,
            l.IsActive, l.CreadoEn,
            inventario?.Existencia, inventario?.Ventas, inventario?.Estado.ToString(),
            l.Ubicacion?.Tipo.ToString(), l.Ubicacion?.Seccion,
            l.Ubicacion?.Estante, l.Ubicacion?.Referencia);
    }
}
