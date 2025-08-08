using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using ProyectoFarmaVita.Components;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.AsignacionTurnoServices;
using ProyectoFarmaVita.Services.CategoriaProductoService;
using ProyectoFarmaVita.Services.CategoriaServices;
using ProyectoFarmaVita.Services.DepartamentoServices;
using ProyectoFarmaVita.Services.DireccionServices;
using ProyectoFarmaVita.Services.EstadoCivilServices;
using ProyectoFarmaVita.Services.GeneroServices;
using ProyectoFarmaVita.Services.InventarioServices;
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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// CAMBIO 1: Cambiar de Scoped a Transient para el servicio
builder.Services.AddTransient<IPersonaService, SPersonaServices>();
builder.Services.AddTransient<ISucursalService, SSucursalService>();
builder.Services.AddTransient<IEstadoCivil, SEstadoCivil>();
builder.Services.AddTransient<IGeneroServices, SGeneroServices>();
builder.Services.AddTransient<IdepartamentoService, SDepartamentoService>();
builder.Services.AddTransient<IMunicipioService, SMunicipioService>();
builder.Services.AddScoped<ITelefonoService, STelefonoService>();
builder.Services.AddScoped<IDireccionService, SDireccionService>();
builder.Services.AddScoped<IInventarioService, SInventarioService>();
builder.Services.AddScoped<ITurnoTrabajoService, STurnoTrabajoService>();
builder.Services.AddScoped<IAsignacionTurnoService, SAsignacionTurnoService>();
builder.Services.AddScoped<ICategoriaService, SCategoriaService>();
builder.Services.AddScoped<IProveedorService, SProveedorService>();
builder.Services.AddScoped<IProductoService, SProductoService>();
builder.Services.AddScoped<IInventarioService, SInventarioService>();

builder.Services.AddMudServices();


// CAMBIO 3: Mantener solo DbContextFactory pero cambiar el ServiceLifetime
builder.Services.AddDbContextFactory<FarmaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
    options.EnableSensitiveDataLogging(true);
    options.UseLazyLoadingProxies(false);
}, ServiceLifetime.Singleton); // Cambiar de Scoped a Singleton

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();