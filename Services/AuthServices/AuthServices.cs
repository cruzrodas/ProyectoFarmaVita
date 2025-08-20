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
        private readonly ILogger<AuthService> _logger;
        private Persona? _currentUser;

        public AuthService(FarmaDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        public Persona? CurrentUser => _currentUser;

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("🔧 AUTH SERVICE: Iniciando login para {Email}", email);

                // ✅ Verificaciones defensivas
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("❌ AUTH: Email vacío");
                    return new AuthResult { Success = false, Message = "Email es requerido" };
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("❌ AUTH: Password vacío");
                    return new AuthResult { Success = false, Message = "Contraseña es requerida" };
                }

                // ✅ Verificar DbContext
                if (_context == null)
                {
                    _logger.LogError("❌ AUTH: DbContext es null");
                    return new AuthResult { Success = false, Message = "Error de configuración de base de datos" };
                }

                _logger.LogInformation("🔧 AUTH: Buscando usuario en base de datos...");

                // ✅ CORREGIDO: Usar el nombre correcto de la tabla y campos
                var user = await _context.Persona
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .FirstOrDefaultAsync(p => p.Email == email.Trim().ToLower() && p.Activo == true);

                if (user == null)
                {
                    _logger.LogWarning("❌ AUTH: Usuario no encontrado para email {Email}", email);
                    return new AuthResult { Success = false, Message = "Usuario no encontrado o inactivo" };
                }

                _logger.LogInformation("✅ AUTH: Usuario encontrado - {Nombre} {Apellido}",
                    user.Nombre, user.Apellido);

                // ✅ CORREGIDO: Verificar contraseña con el campo correcto
                if (!VerifyPassword(password, user.Contraseña))
                {
                    _logger.LogWarning("❌ AUTH: Contraseña incorrecta para {Email}", email);
                    return new AuthResult { Success = false, Message = "Contraseña incorrecta" };
                }

                _logger.LogInformation("✅ AUTH: Contraseña verificada correctamente");

                // ✅ Crear claims de forma segura
                var claims = new List<Claim>();

                try
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.IdPersona.ToString()));
                    claims.Add(new Claim(ClaimTypes.Name, GetFullName(user)));
                    claims.Add(new Claim(ClaimTypes.Email, user.Email ?? ""));
                    claims.Add(new Claim("UserId", user.IdPersona.ToString()));
                    claims.Add(new Claim("FullName", GetFullName(user)));

                    // Agregar rol si existe
                    if (user.IdRoolNavigation?.TipoRol is not null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, user.IdRoolNavigation.TipoRol));
                        claims.Add(new Claim("RoleId", user.IdRoolNavigation.IdRol.ToString()));
                        _logger.LogInformation("✅ AUTH: Rol asignado - {Rol}", user.IdRoolNavigation.TipoRol);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ AUTH: Usuario sin rol asignado");
                    }

                    // Agregar sucursal si existe
                    if (user.IdSucursal.HasValue)
                    {
                        claims.Add(new Claim("SucursalId", user.IdSucursal.Value.ToString()));

                        // Buscar el nombre de la sucursal
                        var sucursal = await _context.Sucursal.FindAsync(user.IdSucursal.Value);
                        if (sucursal?.NombreSucursal is not null)
                        {
                            claims.Add(new Claim("SucursalName", sucursal.NombreSucursal));
                        }
                    }
                }
                catch (Exception claimsEx)
                {
                    _logger.LogError(claimsEx, "❌ AUTH: Error creando claims");
                    return new AuthResult { Success = false, Message = "Error procesando datos del usuario" };
                }

                // ✅ Crear identidad y principal de forma segura
                try
                {
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
                        _logger.LogError("❌ AUTH: HttpContext es null");
                        return new AuthResult { Success = false, Message = "Error interno: HttpContext no disponible" };
                    }

                    _logger.LogInformation("🔧 AUTH: Realizando sign in...");

                    // Autenticar usuario
                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("✅ AUTH: Sign in completado exitosamente");
                }
                catch (Exception signInEx)
                {
                    _logger.LogError(signInEx, "❌ AUTH: Error en SignInAsync");
                    return new AuthResult { Success = false, Message = "Error completando la autenticación" };
                }

                _currentUser = user;

                // ✅ Registrar en bitácora de forma segura
                try
                {
                    await RegistrarAcceso(user, "Login", "Inicio de sesión exitoso");
                    _logger.LogInformation("✅ AUTH: Bitácora registrada");
                }
                catch (Exception bitacoraEx)
                {
                    _logger.LogWarning(bitacoraEx, "⚠️ AUTH: Error registrando bitácora (no crítico)");
                    // No fallar el login por esto
                }

                return new AuthResult
                {
                    Success = true,
                    Message = "Login exitoso",
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 AUTH: Error inesperado en LoginAsync");
                return new AuthResult
                {
                    Success = false,
                    Message = $"Error del sistema: {ex.Message}"
                };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                _logger.LogInformation("🔧 AUTH: Iniciando logout");

                var user = await GetCurrentUserAsync();

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext is not null)
                {
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _logger.LogInformation("✅ AUTH: SignOut completado");
                }

                _currentUser = null;

                // Registrar en bitácora
                if (user is not null)
                {
                    try
                    {
                        await RegistrarAcceso(user, "Logout", "Cierre de sesión");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ AUTH: Error registrando logout en bitácora");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AUTH: Error en logout");
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
                _logger.LogError(ex, "❌ AUTH: Error al obtener usuario actual");
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

        // ✅ Métodos auxiliares privados mejorados
        private bool VerifyPassword(string password, string? hashedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(hashedPassword))
                {
                    _logger.LogWarning("⚠️ AUTH: Hash de contraseña vacío");
                    return false;
                }

                // Por simplicidad, comparamos directamente
                // En producción deberías usar BCrypt o similar
                bool isValid = password == hashedPassword;
                _logger.LogDebug("🔧 AUTH: Verificación de contraseña: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AUTH: Error verificando contraseña");
                return false;
            }
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
                _logger.LogError(ex, "❌ AUTH: Error al registrar en bitácora");
                throw; // Re-lanzar para que el llamador pueda manejar
            }
        }
    }
}