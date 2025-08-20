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
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ? MODERNIZADO: Add services to the container para formularios con estado
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ? CRÍTICO: Agregar RazorPages para formularios con estado persistente
builder.Services.AddRazorPages();

// CONFIGURAR ENTITY FRAMEWORK
builder.Services.AddDbContextFactory<FarmaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.UseLazyLoadingProxies(false);
}, ServiceLifetime.Scoped);

// Configurar MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// Configurar autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Configurar autorización
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

    // Política para redirigir automáticamente al login
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Registrar HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Registrar servicios de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();

// Registrar handlers de autorización
builder.Services.AddScoped<IAuthorizationHandler, ModuleAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SucursalAccessHandler>();

// REGISTRAR SERVICIOS DE NEGOCIO
builder.Services.AddScoped<IPersonaService, SPersonaServices>();
builder.Services.AddScoped<ISucursalService, SSucursalService>();
builder.Services.AddScoped<IEstadoCivil, SEstadoCivil>();
builder.Services.AddScoped<IGeneroServices, SGeneroServices>();
builder.Services.AddScoped<IdepartamentoService, SDepartamentoService>();
builder.Services.AddScoped<IMunicipioService, SMunicipioService>();
builder.Services.AddScoped<ITelefonoService, STelefonoService>();
builder.Services.AddScoped<IDireccionService, SDireccionService>();
builder.Services.AddScoped<ITurnoTrabajoService, STurnoTrabajoService>();
builder.Services.AddScoped<IAsignacionTurnoService, SAsignacionTurnoService>();
builder.Services.AddScoped<ICategoriaService, SCategoriaService>();
builder.Services.AddScoped<IProveedorService, SProveedorService>();
builder.Services.AddScoped<IProductoService, SProductoService>();
builder.Services.AddScoped<IInventarioService, SInventarioService>();

// Agregar HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ? CRÍTICO: UseAntiforgery DEBE ir ANTES de UseAuthentication
app.UseAntiforgery();

// Configurar pipeline de autenticación (ORDEN IMPORTANTE)
app.UseAuthentication();
app.UseAuthorization();

// ? CRÍTICO: MapRazorPages DEBE ir DESPUÉS de UseAntiforgery
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Agregar endpoint de logout
app.MapPost("/logout", async (HttpContext context, IAuthService authService) =>
{
    await authService.LogoutAsync();
    return Results.Redirect("/login");
});

app.Run();