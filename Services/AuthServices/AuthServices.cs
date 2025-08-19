using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using System.Security.Claims;

namespace ProyectoFarmaVita.Services.AuthServices
{
    public class AuthService : IAuthService
    {
        private readonly FarmaDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Persona? _currentUser;

        public AuthService(FarmaDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        public Persona? CurrentUser => _currentUser;

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Email y contraseña son requeridos"
                    };
                }

                // Buscar usuario por email
                var user = await _context.Persona
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .FirstOrDefaultAsync(p => p.Email == email && p.Activo == true);

                if (user is null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Usuario no encontrado o inactivo"
                    };
                }

                // Verificar contraseña
                if (!VerifyPassword(password, user.Contraseña))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Contraseña incorrecta"
                    };
                }

                // Crear claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.IdPersona.ToString()),
                    new Claim(ClaimTypes.Name, GetFullName(user)),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim("UserId", user.IdPersona.ToString()),
                    new Claim("FullName", GetFullName(user)),
                };

                // Agregar rol si existe
                if (user.IdRoolNavigation?.TipoRol is not null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, user.IdRoolNavigation.TipoRol));
                    claims.Add(new Claim("RoleId", user.IdRoolNavigation.IdRol.ToString()));
                }

                // Agregar sucursal si existe
                if (user.IdSucursal.HasValue)
                {
                    claims.Add(new Claim("SucursalId", user.IdSucursal.Value.ToString()));

                    // Buscar el nombre de la sucursal por separado
                    var sucursal = await _context.Sucursal.FindAsync(user.IdSucursal.Value);
                    if (sucursal?.NombreSucursal is not null)
                    {
                        claims.Add(new Claim("SucursalName", sucursal.NombreSucursal));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    IsPersistent = true
                };

                // Verificar que HttpContext no sea null
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext is null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Error interno: HttpContext no disponible"
                    };
                }

                // Autenticar usuario
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _currentUser = user;

                // Registrar en bitácora
                await RegistrarAcceso(user, "Login", "Inicio de sesión exitoso");

                return new AuthResult
                {
                    Success = true,
                    Message = "Login exitoso",
                    User = user
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login: {ex.Message}");
                return new AuthResult
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var user = await GetCurrentUserAsync();

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext is not null)
                {
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }

                _currentUser = null;

                // Registrar en bitácora
                if (user is not null)
                {
                    await RegistrarAcceso(user, "Logout", "Cierre de sesión");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en logout: {ex.Message}");
                return false;
            }
        }

        public async Task<Persona?> GetCurrentUserAsync()
        {
            try
            {
                if (_currentUser is not null)
                    return _currentUser;

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                    return null;

                var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return null;

                _currentUser = await _context.Persona
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .FirstOrDefaultAsync(p => p.IdPersona == userId);

                return _currentUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario actual: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> IsUserInRoleAsync(string role)
        {
            var user = await GetCurrentUserAsync();
            return user?.IdRoolNavigation?.TipoRol?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
        }

        public async Task<List<string>> GetUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            var roles = new List<string>();

            var userRole = user?.IdRoolNavigation?.TipoRol;
            if (!string.IsNullOrEmpty(userRole))
            {
                roles.Add(userRole);
            }

            return roles;
        }

        // Métodos auxiliares privados
        private static bool VerifyPassword(string password, string? hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            // Por simplicidad, comparamos directamente
            // En producción deberías usar BCrypt o similar
            return password == hashedPassword;
        }

        private static string GetFullName(Persona user)
        {
            var nombre = user.Nombre ?? "";
            var apellido = user.Apellido ?? "";
            return $"{nombre} {apellido}".Trim();
        }

        private async Task RegistrarAcceso(Persona usuario, string actividad, string descripcion)
        {
            try
            {
                var bitacora = new Bitacora
                {
                    FechaHora = DateTime.Now,
                    Modulo = "Autenticación",
                    Responsable = GetFullName(usuario),
                    Actividad = actividad,
                    TipoAcceso = 1 // Asumiendo que 1 es login/logout
                };

                _context.Bitacora.Add(bitacora);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar en bitácora: {ex.Message}");
            }
        }
    }
}