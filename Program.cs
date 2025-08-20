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

// Configurar autenticación con cookies
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

        // Eventos para debugging (solo en desarrollo)
        if (builder.Environment.IsDevelopment())
        {
            options.Events = new CookieAuthenticationEvents
            {
                OnSigningIn = context =>
                {
                    Console.WriteLine($"🔧 COOKIE AUTH: Signing in user {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                },
                OnSignedIn = context =>
                {
                    Console.WriteLine($"✅ COOKIE AUTH: User signed in {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                },
                OnSigningOut = context =>
                {
                    Console.WriteLine($"🔧 COOKIE AUTH: Signing out user");
                    return Task.CompletedTask;
                },
                OnValidatePrincipal = context =>
                {
                    Console.WriteLine($"🔧 COOKIE AUTH: Validating principal {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                }
            };
        }
    });

// Configurar autorización con políticas
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

    // Política por defecto: requiere usuario autenticado
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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
// CONFIGURACIÓN DEL PIPELINE HTTP
// ================================================================

// Configurar manejo de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Middleware básico
app.UseHttpsRedirection();
app.UseStaticFiles();

// ⚠️ ORDEN CRÍTICO: UseAntiforgery DEBE ir ANTES de UseAuthentication
app.UseAntiforgery();

// ⚠️ ORDEN CRÍTICO: UseAuthentication DEBE ir ANTES de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

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

// Endpoint de test de autenticación (solo en desarrollo)
if (app.Environment.IsDevelopment())
{
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
}

// ================================================================
// EJECUTAR APLICACIÓN
// ================================================================

app.Run();