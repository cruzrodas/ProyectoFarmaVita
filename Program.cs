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

// NUEVA SINTAXIS .NET 8: Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// CONFIGURAR ENTITY FRAMEWORK
builder.Services.AddDbContextFactory<FarmaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.UseLazyLoadingProxies(false);
}, ServiceLifetime.Scoped);

// Configurar MudBlazor
builder.Services.AddMudServices();

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

    // Políticas basadas en módulos
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery(); // NUEVO en .NET 8

// NUEVA SINTAXIS .NET 8 para assets estáticos
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

// NUEVA SINTAXIS .NET 8: Mapear componentes Razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoint para logout
app.MapPost("/logout", async (IAuthService authService, HttpContext context) =>
{
    await authService.LogoutAsync();
    return Results.Redirect("/login");
});

app.Run();