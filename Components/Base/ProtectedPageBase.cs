using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProyectoFarmaVita.Services.AuthServices;

namespace ProyectoFarmaVita.Components.Base
{
    public abstract class ProtectedPageBase : ComponentBase
    {
        [Inject] protected IAuthService AuthService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        protected string[] RequiredRoles { get; set; } = Array.Empty<string>();
        protected string? RequiredModule { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected bool HasAccess { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await CheckAccess();
            await base.OnInitializedAsync();
        }

        private async Task CheckAccess()
        {
            try
            {
                IsLoading = true;

                if (!AuthService.IsAuthenticated)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                var user = await AuthService.GetCurrentUserAsync();
                if (user == null)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                // Verificar roles si están especificados
                if (RequiredRoles.Any())
                {
                    var userRole = user.IdRoolNavigation?.TipoRol;
                    if (string.IsNullOrEmpty(userRole) || !RequiredRoles.Contains(userRole))
                    {
                        NavigationManager.NavigateTo("/access-denied");
                        return;
                    }
                }

                // Verificar acceso al módulo si está especificado
                if (!string.IsNullOrEmpty(RequiredModule))
                {
                    var hasModuleAccess = await CheckModuleAccess(user.IdRoolNavigation?.TipoRol, RequiredModule);
                    if (!hasModuleAccess)
                    {
                        NavigationManager.NavigateTo("/access-denied");
                        return;
                    }
                }

                HasAccess = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando acceso: {ex.Message}");
                NavigationManager.NavigateTo("/access-denied");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private static Task<bool> CheckModuleAccess(string? userRole, string module)
        {
            var hasAccess = module switch
            {
                "Personas" => userRole == "Administrador" || userRole == "Gerente",
                "Productos" => userRole == "Administrador" || userRole == "Gerente" || userRole == "Farmaceuta",
                "Inventarios" => userRole == "Administrador" || userRole == "Gerente" || userRole == "Farmaceuta",
                "Ventas" => userRole == "Administrador" || userRole == "Gerente" || userRole == "Farmaceuta" || userRole == "Vendedor",
                "Reportes" => userRole == "Administrador" || userRole == "Gerente",
                "Configuracion" => userRole == "Administrador",
                _ => userRole == "Administrador"
            };

            return Task.FromResult(hasAccess);
        }

        protected virtual async Task OnAccessGranted()
        {
            // Método virtual que pueden sobrescribir las páginas derivadas
            await Task.CompletedTask;
        }
    }

    // Atributo para marcar páginas que requieren roles específicos
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireRoleAttribute : Attribute
    {
        public string[] Roles { get; }

        public RequireRoleAttribute(params string[] roles)
        {
            Roles = roles;
        }
    }

    // Atributo para marcar páginas que requieren acceso a módulos específicos
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireModuleAttribute : Attribute
    {
        public string Module { get; }

        public RequireModuleAttribute(string module)
        {
            Module = module;
        }
    }
}