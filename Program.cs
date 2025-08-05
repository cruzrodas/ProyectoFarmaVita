using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using ProyectoFarmaVita.Components;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.PersonaServices;
using ProyectoFarmaVita.Services.SucursalServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// CAMBIO 1: Cambiar de Scoped a Transient para el servicio
builder.Services.AddTransient<IPersonaService, SPersonaServices>();
builder.Services.AddTransient<ISucursalService, SSucursalServices>();
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