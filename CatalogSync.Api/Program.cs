using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CatalogSync.Infrastructure.Extensions;
using CatalogSync.Persistence;
using CatalogSync.Persistence.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using Serilog;
using Serilog.Events;

ExcelPackage.License.SetNonCommercialPersonal("CatalogSync");

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14
    ));

// ── Persistence ──────────────────────────────────────────────────────────────
builder.Services.AddPersistence(builder.Configuration);

// ── Cache ─────────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Auth ─────────────────────────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt["Issuer"],
            ValidAudience            = jwt["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var sub          = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var versionClaim = ctx.Principal?.FindFirstValue("tokenVersion");

                if (!Guid.TryParse(sub, out var userId) || !int.TryParse(versionClaim, out var claimVersion))
                {
                    ctx.Fail("Token inválido.");
                    return;
                }

                var cache    = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                var cacheKey = $"tv_{userId}";

                if (!cache.TryGetValue<int>(cacheKey, out var dbVersion))
                {
                    var db      = ctx.HttpContext.RequestServices.GetRequiredService<LibreriaDbContext>();
                    var usuario = await db.Usuarios.FindAsync(userId);
                    if (usuario is null) { ctx.Fail("Usuario no encontrado."); return; }
                    dbVersion = usuario.TokenVersion;
                    cache.Set(cacheKey, dbVersion, TimeSpan.FromSeconds(30));
                }

                if (claimVersion != dbVersion)
                    ctx.Fail("Sesión invalidada. Inicia sesión nuevamente.");
            }
        };
    });

builder.Services.AddAuthorization();

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window      = TimeSpan.FromMinutes(1);
        opt.QueueLimit  = 0;
    });

    o.AddFixedWindowLimiter("publica", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window      = TimeSpan.FromMinutes(1);
        opt.QueueLimit  = 0;
    });

    o.AddFixedWindowLimiter("bulk", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window      = TimeSpan.FromMinutes(1);
        opt.QueueLimit  = 0;
    });

    o.RejectionStatusCode = 429;
});

// ── MVC + OpenAPI ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(o =>
    o.AddPolicy("Angular", p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ── Infrastructure (services) ─────────────────────────────────────────────────
builder.Services.AddInfrastructure();

var app = builder.Build();

// ── Auto-migrate + seed admin ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db   = scope.ServiceProvider.GetRequiredService<LibreriaDbContext>();
    var cfg  = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    db.Database.Migrate();

    var adminCfg   = cfg.GetSection("AdminSeed");
    var adminEmail = adminCfg["Email"];
    var adminPass  = adminCfg["Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPass))
    {
        var email = adminEmail.ToLowerInvariant().Trim();
        if (!db.Usuarios.Any(u => u.Role == CatalogSync.Domain.Enums.Role.Admin))
        {
            var hash  = BCrypt.Net.BCrypt.HashPassword(adminPass);
            var admin = CatalogSync.Domain.Entities.Usuario.Create(
                email, hash, adminCfg["NombreCompleto"] ?? "Administrador",
                CatalogSync.Domain.Enums.Role.Admin);
            db.Usuarios.Add(admin);
            db.SaveChanges();
        }
    }
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    ctx.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseCors("Angular");

var webRootPath = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath)
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapControllers();
app.Run();
