using Microsoft.AspNetCore.Authorization;
using ProyectoFarmaVita.Services.AuthServices;

namespace ProyectoFarmaVita.Authorization
{
    // Requerimiento para acceso a módulos específicos
    public class ModuleAccessRequirement : IAuthorizationRequirement
    {
        public string ModuleName { get; }
        public ModuleAccessRequirement(string moduleName)
        {
            ModuleName = moduleName;
        }
    }

    // Requerimiento para acceso a sucursal específica
    public class SucursalAccessRequirement : IAuthorizationRequirement
    {
        public int? SucursalId { get; }
        public SucursalAccessRequirement(int? sucursalId = null)
        {
            SucursalId = sucursalId;
        }
    }

    // Handler para acceso a módulos
    public class ModuleAccessHandler : AuthorizationHandler<ModuleAccessRequirement>
    {
        private readonly IAuthService _authService;

        public ModuleAccessHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ModuleAccessRequirement requirement)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                context.Fail();
                return;
            }

            var userRole = user.IdRoolNavigation?.TipoRol;
            var hasAccess = requirement.ModuleName switch
            {
                "Personas" => userRole == "Administrador" || userRole == "Gerente",
                "Productos" => userRole == "Administrador" || userRole == "Gerente" || userRole == "Farmaceuta",
                "Inventarios" => userRole == "Administrador" || userRole == "Gerente" || userRole == "Farmaceuta",
                "Ventas" => userRole == "Administrador" || userRole == "Gerente" || userRole == "Farmaceuta" || userRole == "Vendedor",
                "Reportes" => userRole == "Administrador" || userRole == "Gerente",
                "Configuracion" => userRole == "Administrador",
                _ => userRole == "Administrador"
            };

            if (hasAccess)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    // Handler para acceso a sucursal
    public class SucursalAccessHandler : AuthorizationHandler<SucursalAccessRequirement>
    {
        private readonly IAuthService _authService;

        public SucursalAccessHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SucursalAccessRequirement requirement)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                context.Fail();
                return;
            }

            var userRole = user.IdRoolNavigation?.TipoRol;

            // Administradores y Gerentes tienen acceso a todas las sucursales
            if (userRole == "Administrador" || userRole == "Gerente")
            {
                context.Succeed(requirement);
                return;
            }

            // Otros roles solo pueden acceder a su sucursal asignada
            if (requirement.SucursalId == null || user.IdSucursal == requirement.SucursalId)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    // Políticas de autorización
    public static class AuthorizationPolicies
    {
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequireManagerRole = "RequireManagerRole";
        public const string RequirePharmaRole = "RequirePharmaRole";
        public const string RequireSalesRole = "RequireSalesRole";

        public const string PersonasAccess = "PersonasAccess";
        public const string ProductosAccess = "ProductosAccess";
        public const string InventariosAccess = "InventariosAccess";
        public const string VentasAccess = "VentasAccess";
        public const string ReportesAccess = "ReportesAccess";
        public const string ConfiguracionAccess = "ConfiguracionAccess";
    }
}