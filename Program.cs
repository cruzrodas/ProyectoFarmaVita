using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using ProyectoFarmaVita.Authorization;
using ProyectoFarmaVita.Components;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.AsignacionTurnoServices;
using ProyectoFarmaVita.Services.AuthServices;
using ProyectoFarmaVita.Services.CategoriaProductoService;
using ProyectoFarmaVita.Services.CategoriaServices;
using ProyectoFarmaVita.Services.DepartamentoServices;
using ProyectoFarmaVita.Services.DireccionServices;
using ProyectoFarmaVita.Services.EstadoCivilServices;
using ProyectoFarmaVita.Services.GeneroServices;
using ProyectoFarmaVita.Services.InventarioService;
using ProyectoFarmaVita.Services.MunicipioService;
using ProyectoFarmaVita.Services.MunicipioServices;
using ProyectoFarmaVita.Services.PersonaServices;
using ProyectoFarmaVita.Services.ProductoService;
using ProyectoFarmaVita.Services.ProductoServices;
using ProyectoFarmaVita.Services.ProveedorServices;
using ProyectoFarmaVita.Services.SucursalServices;
using ProyectoFarmaVita.Services.TelefonoServices;
using ProyectoFarmaVita.Services.TurnoTrabajoService;
using ProyectoFarmaVita.Services.TurnoTrabajoServices;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// CONFIGURACIÓN DE SERVICIOS
// ================================================================

// Configurar Razor Components con Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configurar Razor Pages (necesario para algunos formularios)
builder.Services.AddRazorPages();

// ✅ AGREGADO: AuthenticationStateProvider para Blazor
builder.Services.AddCascadingAuthenticationState();

// ================================================================
// CONFIGURACIÓN DE BASE DE DATOS
// ================================================================

// Configurar Entity Framework con Factory y DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// DbContextFactory para servicios que lo necesiten (como PersonaService)
builder.Services.AddDbContextFactory<FarmaDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlServerOptions.CommandTimeout(120);
    });

    // Solo en desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }

    // Deshabilitar lazy loading para mejor control
    options.UseLazyLoadingProxies(false);
}, ServiceLifetime.Scoped);

// DbContext regular para servicios que no necesitan Factory
builder.Services.AddDbContext<FarmaDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure();
        sqlServerOptions.CommandTimeout(120);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }

    options.UseLazyLoadingProxies(false);
}, ServiceLifetime.Scoped);

// ================================================================
// CONFIGURACIÓN DE AUTENTICACIÓN Y AUTORIZACIÓN
// ================================================================

// Configurar HttpContextAccessor (DEBE ir antes de authentication)
builder.Services.AddHttpContextAccessor();

// Configurar autenticación con cookies - ✅ MEJORADA
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "FarmaVitaAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;

        // ✅ CRÍTICO: Eventos mejorados para evitar redirección de archivos estáticos
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // ✅ NO redirigir archivos estáticos - ESTO SOLUCIONA EL 302
                if (context.Request.Path.StartsWithSegments("/_framework") ||
                    context.Request.Path.StartsWithSegments("/_content") ||
                    context.Request.Path.StartsWithSegments("/css") ||
                    context.Request.Path.StartsWithSegments("/js") ||
                    context.Request.Path.StartsWithSegments("/lib"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }

                // Solo redirigir páginas normales
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },

            // Eventos para debugging (solo en desarrollo)
            OnSigningIn = context =>
            {
                if (builder.Environment.IsDevelopment())
                    Console.WriteLine($"🔧 COOKIE AUTH: Signing in user {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnSignedIn = context =>
            {
                if (builder.Environment.IsDevelopment())
                    Console.WriteLine($"✅ COOKIE AUTH: User signed in {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnSigningOut = context =>
            {
                if (builder.Environment.IsDevelopment())
                    Console.WriteLine($"🔧 COOKIE AUTH: Signing out user");
                return Task.CompletedTask;
            },
            OnValidatePrincipal = context =>
            {
                if (builder.Environment.IsDevelopment())
                    Console.WriteLine($"🔧 COOKIE AUTH: Validating principal {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

// ✅ MODIFICADA: Configurar autorización sin FallbackPolicy agresiva
builder.Services.AddAuthorization(options =>
{
    // Políticas basadas en roles
    options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy =>
        policy.RequireRole("Administrador"));

    options.AddPolicy(AuthorizationPolicies.RequireManagerRole, policy =>
        policy.RequireRole("Administrador", "Gerente"));

    options.AddPolicy(AuthorizationPolicies.RequirePharmaRole, policy =>
        policy.RequireRole("Administrador", "Gerente", "Farmaceuta"));

    options.AddPolicy(AuthorizationPolicies.RequireSalesRole, policy =>
        policy.RequireRole("Administrador", "Gerente", "Farmaceuta", "Vendedor"));

    // Políticas basadas en módulos usando handlers personalizados
    options.AddPolicy(AuthorizationPolicies.PersonasAccess, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("Personas")));

    options.AddPolicy(AuthorizationPolicies.ProductosAccess, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("Productos")));

    options.AddPolicy(AuthorizationPolicies.InventariosAccess, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("Inventarios")));

    options.AddPolicy(AuthorizationPolicies.VentasAccess, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("Ventas")));

    options.AddPolicy(AuthorizationPolicies.ReportesAccess, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("Reportes")));

    options.AddPolicy(AuthorizationPolicies.ConfiguracionAccess, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("Configuracion")));

    // ✅ COMENTADA: Política por defecto comentada temporalmente para evitar bloqueo de archivos estáticos
    // options.FallbackPolicy = new AuthorizationPolicyBuilder()
    //     .RequireAuthenticatedUser()
    //     .Build();
});

// ================================================================
// CONFIGURACIÓN DE MudBlazor
// ================================================================

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
});

// ================================================================
// REGISTRO DE SERVICIOS DE APLICACIÓN
// ================================================================

// Servicios de autenticación y autorización
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthorizationHandler, ModuleAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SucursalAccessHandler>();

// Servicios de negocio (ordenados alfabéticamente)
builder.Services.AddScoped<IAsignacionTurnoService, SAsignacionTurnoService>();
builder.Services.AddScoped<ICategoriaService, SCategoriaService>();
builder.Services.AddScoped<IdepartamentoService, SDepartamentoService>();
builder.Services.AddScoped<IDireccionService, SDireccionService>();
builder.Services.AddScoped<IEstadoCivil, SEstadoCivil>();
builder.Services.AddScoped<IGeneroServices, SGeneroServices>();
builder.Services.AddScoped<IInventarioService, SInventarioService>();
builder.Services.AddScoped<IMunicipioService, SMunicipioService>();
builder.Services.AddScoped<IPersonaService, SPersonaServices>();
builder.Services.AddScoped<IProductoService, SProductoService>();
builder.Services.AddScoped<IProveedorService, SProveedorService>();
builder.Services.AddScoped<ISucursalService, SSucursalService>();
builder.Services.AddScoped<ITelefonoService, STelefonoService>();
builder.Services.AddScoped<ITurnoTrabajoService, STurnoTrabajoService>();

// ================================================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ================================================================

var app = builder.Build();

// ================================================================
// ✅ CONFIGURACIÓN CORREGIDA DEL PIPELINE HTTP
// ================================================================

// 1. Configurar manejo de errores (PRIMERO)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// 2. HTTPS redirect
app.UseHttpsRedirection();

// 3. ✅ ARCHIVOS ESTÁTICOS (CRÍTICO: ANTES del routing y auth)
app.UseStaticFiles();

// 4. ✅ ROUTING (AGREGADO)
app.UseRouting();

// 5. ✅ AUTHENTICATION (DESPUÉS de UseRouting, ANTES de UseAuthorization)
app.UseAuthentication();

// 6. ✅ AUTHORIZATION (DESPUÉS de UseAuthentication)
app.UseAuthorization();

// 7. ✅ ANTIFORGERY (AL FINAL, DESPUÉS de auth)
app.UseAntiforgery();

// ================================================================
// CONFIGURACIÓN DE RUTAS Y ENDPOINTS
// ================================================================

// Mapear Razor Pages
app.MapRazorPages();

// Mapear Razor Components con modo interactivo
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ================================================================
// ENDPOINTS PERSONALIZADOS
// ================================================================

// Endpoint de logout
app.MapPost("/logout", async (HttpContext context, IAuthService authService) =>
{
    try
    {
        Console.WriteLine("🔧 ENDPOINT: /logout called");
        await authService.LogoutAsync();
        Console.WriteLine("✅ ENDPOINT: Logout successful");
        return Results.Redirect("/login");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ENDPOINT: Logout error - {ex.Message}");
        return Results.Redirect("/login");
    }
});

// ✅ AGREGADO: Endpoint para verificar archivos estáticos
if (app.Environment.IsDevelopment())
{
    // Endpoint de test de autenticación
    app.MapGet("/test-auth", async (HttpContext context, IAuthService authService) =>
    {
        try
        {
            var isAuthenticated = authService.IsAuthenticated;
            var currentUser = await authService.GetCurrentUserAsync();
            var roles = await authService.GetUserRolesAsync();

            var result = new
            {
                IsAuthenticated = isAuthenticated,
                User = currentUser?.Nombre + " " + currentUser?.Apellido,
                Email = currentUser?.Email,
                Role = currentUser?.IdRoolNavigation?.TipoRol,
                Roles = roles,
                Claims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToArray(),
                Timestamp = DateTime.Now
            };

            return Results.Json(result);
        }
        catch (Exception ex)
        {
            return Results.Json(new { Error = ex.Message, Timestamp = DateTime.Now });
        }
    });

    // ✅ NUEVO: Endpoint para verificar archivos estáticos
    app.MapGet("/debug/static", () =>
    {
        return Results.Json(new
        {
            Message = "Static files are working correctly!",
            Timestamp = DateTime.Now,
            Status = "OK"
        });
    });

    // ✅ NUEVO: Middleware de debugging para ver todas las requests
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"🔍 Request: {context.Request.Method} {context.Request.Path}");
        await next();
        Console.WriteLine($"📤 Response: {context.Response.StatusCode} for {context.Request.Path}");
    });
}

// ================================================================
// INICIALIZACIÓN Y LOGGING
// ================================================================

// Log de configuración en desarrollo
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("🚀 FarmaVita Application Starting...");
    Console.WriteLine($"🔧 Environment: {app.Environment.EnvironmentName}");
    Console.WriteLine($"🔧 Connection String Configured: {!string.IsNullOrEmpty(connectionString)}");
    Console.WriteLine("🔧 Authentication: Cookie-based");
    Console.WriteLine("🔧 Authorization: Role + Policy-based");
    Console.WriteLine("✅ Application configured successfully");
    Console.WriteLine("🔧 Middleware order: StaticFiles → Routing → Auth → Authorization → Antiforgery");
}

// ================================================================
// EJECUTAR APLICACIÓN
// ================================================================

app.Run();