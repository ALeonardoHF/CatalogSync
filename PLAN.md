# Librería Atenas — Plan de Proyecto

## 1. Descripción

App interna para automatizar la actualización de precios del catálogo de libros.
Sube el catálogo maestro (`existencias.xls`) y los catálogos de proveedores
(`OCEANO.xls`, `PLANETA.xls`) y aplica las reglas de negocio automáticamente.

**Stack:** ASP.NET Core Web API (.NET 9) + Angular 19

---

## 2. Análisis de los Archivos Excel

### existencias.xls — Catálogo maestro
| Col | Campo        | Notas                                  |
|-----|--------------|----------------------------------------|
| 1   | ISBN         | Puede tener `*` o `**` como prefijo/sufijo |
| 2   | TITULO       |                                        |
| 3   | AUTOR        |                                        |
| 4   | DesCuento    | Decimal                                |
| 5   | EDITORIAL    |                                        |
| 6   | COSTO        | Decimal                                |
| 7   | inc.         |                                        |
| 8   | PRECIO       | **Campo clave para comparar**          |
| 9   | FENTRADA     | Fecha                                  |
| 10  | CodigoBArrAs |                                        |
| 11  | EXISTENCIA   | Int — stock actual                     |
| 12  | VENTAS       | Int                                    |

- ~56,000 filas
- Fila 1 = encabezados, datos desde fila 2
- ISBNs sucios: `*9789684630444*`, `**`, espacios → hay que normalizar

### OCEANO.xls — Proveedor
| Col | Campo           |
|-----|-----------------|
| 1   | id              |
| 2   | (vacía)         |
| 3   | ISBN            |
| 4   | NOMBRE DEL LIBRO|
| 5   | AUTOR           |
| 6   | EDITORIAL       |
| 7   | PRECIO UNITARIO |

- ~6,700 filas
- Fila 1 = encabezados, fila 2 vacía, **datos desde fila 3**

### PLANETA CONSIGNACION.xls — Proveedor
| Col | Campo           |
|-----|-----------------|
| 1   | id              |
| 2   | (vacía)         |
| 3   | ISBN            |
| 4   | NOMBRE DEL LIBRO|
| 5   | AUTOR (vacío)   |
| 6   | PRECIO UNITARIO |
| 7   | NOMBRE DEL LIBRO (duplicado) |
| 8   | AUTOR (duplicado) |

- ~6,700 filas
- Mismo patrón que OCEANO: fila 3 = primer dato
- AUTOR generalmente vacío

---

## 3. Reglas de Negocio

```
Para cada libro de proveedor (OCEANO o PLANETA):

  isbn_normalizado = isbn.Trim().Trim('*').Trim()

  SI isbn existe en catálogo:
    SI precio_proveedor > precio_catalogo:
      → actualizar precio  (registrar: "Actualizado")
    SINO:
      → no tocar           (registrar: "Sin cambio")

  SI isbn NO existe en catálogo:
    → agregar como nuevo registro con datos del proveedor
      (registrar: "Nuevo")
```

**Lógica:** siempre gana el precio más alto (protege margen de la librería).

**Normalización de ISBN:**
```csharp
string.Trim().Trim('*').Trim()
// "*9789684630444*" → "9789684630444"
// "  9789684630444 " → "9789684630444"
// "**" → "" (ignorar)
```

---

## 4. Reutilización desde auth-front

### ✅ Se puede reutilizar (copiar/adaptar)

| Archivo | Qué copiar |
|---------|-----------|
| `styles.css` | Todo — el tema dark/purple ya está listo |
| `notification.service.ts` | Toast de éxito/error |
| `loading-button.directive.ts` | Estado loading en botones |
| `environment.ts` / `environment.development.ts` | Patrón de environments |
| `api-url.token.ts` | InjectionToken para la URL de la API |
| Estructura de carpetas `core/`, `features/`, `shared/` | Convención |
| Patrón `inject()` + signals + standalone components | Convención Angular 19 |

### ❌ No aplica (no copiar)

- Guards (auth, role, unsavedChanges) — no hay login
- Interceptors (auth, token-refresh) — no hay JWT
- Modelos de usuario/auth — dominio diferente
- user-table component — estructura diferente
- Todos los features: login, register, 2FA, dashboard, profile

### Conclusión
El nuevo proyecto Angular arranca desde `ng new` pero copiamos `styles.css`
y 3-4 servicios/directivas. **No usamos auth-front como base**, son apps separadas.

---

## 5. Arquitectura

```
CatalogSync/
├── PLAN.md                          ← este archivo
├── existencias.xls                  ← datos de prueba
├── OCEANO.xls
├── PLANETA CONSIGNACION.xls
│
├── CatalogSync.Api/              ← ASP.NET Core Web API
│   ├── Controllers/
│   │   └── CatalogoController.cs
│   ├── Models/
│   │   ├── LibroExistencia.cs
│   │   ├── LibroProveedor.cs
│   │   └── ResumenProceso.cs
│   ├── Services/
│   │   ├── IExcelService.cs
│   │   ├── ExcelService.cs          ← leer/escribir con EPPlus
│   │   └── CatalogoService.cs       ← reglas de negocio
│   └── Program.cs
│
└── libreria-front/                  ← Angular 19
    └── src/app/
        ├── core/
        │   ├── services/
        │   │   ├── catalogo.service.ts
        │   │   └── notification.service.ts  (copiado)
        │   └── tokens/
        │       └── api-url.token.ts          (copiado)
        ├── features/
        │   └── catalogo/
        │       └── catalogo.component.ts     ← pantalla principal
        └── shared/
            └── directives/
                └── loading-button.directive.ts (copiado)
```

---

## 6. Modelos de Datos

### C# — Backend

```csharp
// Models/LibroExistencia.cs
public class LibroExistencia
{
    public string ISBN { get; set; } = "";
    public string ISBNOriginal { get; set; } = ""; // con asteriscos, para escribir igual
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Descuento { get; set; } = "";
    public string Editorial { get; set; } = "";
    public string Costo { get; set; } = "";
    public string Inc { get; set; } = "";
    public decimal Precio { get; set; }
    public string FechaEntrada { get; set; } = "";
    public string CodigoBarra { get; set; } = "";
    public string Existencia { get; set; } = "";
    public string Ventas { get; set; } = "";
    public int FilaOriginal { get; set; }  // para escribir en la misma fila
}

// Models/LibroProveedor.cs
public class LibroProveedor
{
    public string ISBN { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Editorial { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public string Proveedor { get; set; } = ""; // "OCEANO" | "PLANETA"
}

// Models/ResumenProceso.cs
public class ResumenProceso
{
    public int TotalCatalogo { get; set; }
    public int Actualizados { get; set; }
    public int SinCambio { get; set; }
    public int Nuevos { get; set; }
    public int ISBNsInvalidos { get; set; }
    public string ArchivoId { get; set; } = ""; // GUID para descargar
    public List<DetalleCambio> Cambios { get; set; } = [];
}

public class DetalleCambio
{
    public string ISBN { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public string Resultado { get; set; } = ""; // "Actualizado" | "Nuevo" | "SinCambio"
}
```

### TypeScript — Frontend

```typescript
// models/resumen-proceso.model.ts
export interface ResumenProceso {
  totalCatalogo: number;
  actualizados: number;
  sinCambio: number;
  nuevos: number;
  isBNsInvalidos: number;
  archivoId: string;
  cambios: DetalleCambio[];
}

export interface DetalleCambio {
  isbn: string;
  titulo: string;
  proveedor: string;
  precioAnterior: number;
  precioNuevo: number;
  resultado: 'Actualizado' | 'Nuevo' | 'SinCambio';
}
```

---

## 7. API Endpoints

### POST /api/catalogo/procesar
Recibe los archivos Excel y aplica las reglas de negocio.

**Request:** `multipart/form-data`
```
existencias    (file, required)  .xls/.xlsx
oceano         (file, optional)  .xls/.xlsx
planeta        (file, optional)  .xls/.xlsx
```

**Response 200:**
```json
{
  "totalCatalogo": 56200,
  "actualizados": 312,
  "sinCambio": 5890,
  "nuevos": 45,
  "isBNsInvalidos": 3,
  "archivoId": "a3f7c9d2-...",
  "cambios": [
    {
      "isbn": "9789684630444",
      "titulo": "El nombre del viento",
      "proveedor": "OCEANO",
      "precioAnterior": 350.00,
      "precioNuevo": 420.00,
      "resultado": "Actualizado"
    }
  ]
}
```

### GET /api/catalogo/descargar/{archivoId}
Descarga el Excel procesado.

**Response:** `application/octet-stream` (archivo .xlsx)

---

## 8. Flujo de la Aplicación

```
[Angular - pantalla única]

┌─────────────────────────────────────────────────────┐
│  Librería Atenas — Actualización de Catálogo        │
│                                                     │
│  📁 existencias.xls    [Seleccionar archivo] ✓     │
│  📁 OCEANO.xls         [Seleccionar archivo] ✓     │
│  📁 PLANETA.xls        [Seleccionar archivo]       │
│                                                     │
│              [Procesar Catálogo  ⟳]                │
└─────────────────────────────────────────────────────┘

           ↓ POST multipart/form-data

┌─────────────────────────────────────────────────────┐
│  Resultado del proceso                              │
│                                                     │
│  Total en catálogo:    56,200                       │
│  ✅ Actualizados:         312                       │
│  ➕ Nuevos:                45                       │
│  — Sin cambio:          5,890                       │
│  ⚠ ISBNs inválidos:        3                       │
│                                                     │
│  [Descargar existencias_actualizado.xlsx]           │
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │ ISBN          │ Título   │ Antes │ Ahora │ ? │  │
│  │ 9789684630444 │ El nom…  │ $350  │ $420  │ ↑ │  │
│  │ 9788408061328 │ ¡Cuida…  │  —    │  $99  │ + │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

---

## 9. Plan de Implementación

### Fase 1 — Backend (API)
- [ ] Crear proyecto `CatalogSync.Api` (Web API .NET 9)
- [ ] Agregar EPPlus (NuGet)
- [ ] Configurar CORS para Angular (localhost:4200)
- [ ] `ExcelService` — leer existencias.xls
- [ ] `ExcelService` — leer OCEANO / PLANETA (misma lógica, parámetro "fila inicio")
- [ ] `CatalogoService` — normalizar ISBNs
- [ ] `CatalogoService` — aplicar reglas de precio
- [ ] `ExcelService` — escribir resultado en nuevo .xlsx
- [ ] `CatalogoController` — endpoint `/procesar`
- [ ] `CatalogoController` — endpoint `/descargar/{id}`
- [ ] Probar con Postman/Swagger

### Fase 2 — Frontend (Angular)
- [ ] Crear proyecto `libreria-front` (Angular 19)
- [ ] Copiar `styles.css` de auth-front
- [ ] Copiar `notification.service.ts` + `loading-button.directive.ts`
- [ ] `CatalogoService` — llamada HTTP a la API
- [ ] `CatalogoComponent` — inputs de archivo + botón procesar
- [ ] `CatalogoComponent` — mostrar resumen con tarjetas de estadísticas
- [ ] `CatalogoComponent` — tabla de cambios filtrable
- [ ] Botón de descarga del Excel resultado

### Fase 3 — Detalles finales
- [ ] Validación de archivos (solo .xls/.xlsx, tamaño máximo)
- [ ] Manejo de errores (archivo corrupto, ISBN no encontrado, etc.)
- [ ] Loading state mientras procesa (puede tardar con 56k filas)
- [ ] Probar con los archivos reales

---

## 10. Consideraciones Técnicas

### EPPlus vs ClosedXML
Usamos **EPPlus** porque:
- Soporta .xls y .xlsx
- Más rápido para 56,000 filas
- No requiere Office instalado (evita el crash de COM Interop)
- Licencia NonCommercial gratuita (app interna = ok)

### Procesamiento de 56k filas
El catálogo tiene 56,260 filas. La estrategia:
1. Cargar todo en un `Dictionary<string, LibroExistencia>` (clave = ISBN normalizado)
2. Iterar proveedores (~13k filas total) contra ese diccionario
3. No leer/escribir fila por fila en Excel — todo en memoria primero

### Archivos temporales
El resultado procesado se guarda en memoria (MemoryStream) identificado por GUID.
El frontend descarga inmediatamente después del proceso. No hay persistencia en disco.

### Formato de salida
El archivo resultante es `.xlsx` (no `.xls`) para mayor compatibilidad.
Las columnas y el orden son idénticos a `existencias.xls` original.
