using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.PersonaServices;

namespace ProyectoFarmaVita.Services.PersonaServices
{
    public class SPersonaServices : IPersonaService
    {
        private readonly IDbContextFactory<FarmaDbContext> _contextFactory;

        public SPersonaServices(IDbContextFactory<FarmaDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<(List<Persona> personas, int totalCount)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            string searchTerm = "",
            bool sortAscending = true,
            string sortBy = "Nombre",
            bool mostrarInactivos = false,
            int? rolId = null,
            int? sucursalId = null,
            int? generoId = null,
            int? estadoCivilId = null)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.Persona
                    .Include(p => p.IdDireccionNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsQueryable();

                // Filtro por estado activo/inactivo
                if (mostrarInactivos)
                {
                    query = query.Where(p => p.Activo == false);
                }
                else
                {
                    query = query.Where(p => p.Activo == true);
                }

                // Aplicar búsqueda
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    query = query.Where(p =>
                        EF.Functions.Like(p.Nombre.ToLower(), $"%{searchLower}%") ||
                        EF.Functions.Like(p.Apellido.ToLower(), $"%{searchLower}%") ||
                        EF.Functions.Like(p.Email.ToLower(), $"%{searchLower}%") ||
                        (p.Dpi.HasValue && EF.Functions.Like(p.Dpi.ToString(), $"%{searchTerm}%")));
                }

                // Aplicar filtros adicionales
                if (rolId.HasValue)
                    query = query.Where(p => p.IdRool == rolId.Value);

                if (sucursalId.HasValue)
                    query = query.Where(p => p.IdSucursal == sucursalId.Value);

                if (generoId.HasValue)
                    query = query.Where(p => p.IdGenero == generoId.Value);

                if (estadoCivilId.HasValue)
                    query = query.Where(p => p.IdEstadoCivil == estadoCivilId.Value);

                // Aplicar ordenamiento
                query = sortBy.ToLower() switch
                {
                    "nombre" => sortAscending ? query.OrderBy(p => p.Nombre) : query.OrderByDescending(p => p.Nombre),
                    "apellido" => sortAscending ? query.OrderBy(p => p.Apellido) : query.OrderByDescending(p => p.Apellido),
                    "email" => sortAscending ? query.OrderBy(p => p.Email) : query.OrderByDescending(p => p.Email),
                    "fechacreacion" => sortAscending ? query.OrderBy(p => p.FechaCreacion) : query.OrderByDescending(p => p.FechaCreacion),
                    "idpersona" => sortAscending ? query.OrderBy(p => p.IdPersona) : query.OrderByDescending(p => p.IdPersona),
                    _ => sortAscending ? query.OrderBy(p => p.Nombre) : query.OrderByDescending(p => p.Nombre)
                };

                // Ejecutar consultas de forma paralela para mejor rendimiento
                var countTask = query.CountAsync();
                var dataTask = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking() // Mejora significativa de rendimiento
                    .ToListAsync();

                await Task.WhenAll(countTask, dataTask);

                return (await dataTask, await countTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPaginatedAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (new List<Persona>(), 0);
            }
        }

        public async Task<bool> AddAsync(Persona persona)
        {
            try
            {
                if (persona == null) return false;
                if (string.IsNullOrEmpty(persona.Nombre)) return false;
                if (string.IsNullOrEmpty(persona.Email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                // Verificar email único
                var emailExists = await context.Persona.AnyAsync(p => p.Email == persona.Email);
                if (emailExists) return false;

                // Verificar DPI único si se proporciona
                if (persona.Dpi.HasValue)
                {
                    var dpiExists = await context.Persona.AnyAsync(p => p.Dpi == persona.Dpi.Value);
                    if (dpiExists) return false;
                }

                // Configurar campos automáticos
                persona.FechaCreacion = DateTime.Now;
                persona.FechaRegistro = DateTime.Now.ToString("yyyy-MM-dd");
                persona.Activo = persona.Activo ?? true;

                // TODO: En producción, implementar hash de contraseña
                // if (!string.IsNullOrEmpty(persona.Contraseña))
                // {
                //     persona.Contraseña = BCrypt.Net.BCrypt.HashPassword(persona.Contraseña);
                // }

                context.Persona.Add(persona);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Persona persona)
        {
            try
            {
                if (persona == null) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                var existingPersona = await context.Persona
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPersona == persona.IdPersona);

                if (existingPersona == null) return false;

                // Verificar email único (excluyendo la persona actual)
                if (!string.IsNullOrEmpty(persona.Email))
                {
                    var emailExists = await context.Persona
                        .AsNoTracking()
                        .AnyAsync(p => p.Email == persona.Email && p.IdPersona != persona.IdPersona);

                    if (emailExists) return false;
                }

                // Verificar DPI único (excluyendo la persona actual)
                if (persona.Dpi.HasValue)
                {
                    var dpiExists = await context.Persona
                        .AsNoTracking()
                        .AnyAsync(p => p.Dpi == persona.Dpi.Value && p.IdPersona != persona.IdPersona);

                    if (dpiExists) return false;
                }

                // Conservar datos importantes del registro original
                persona.FechaCreacion = existingPersona.FechaCreacion;
                persona.UsuarioCreacion = existingPersona.UsuarioCreacion;
                persona.FechaRegistro = existingPersona.FechaRegistro;

                // Actualizar fecha de modificación
                persona.FechaModificacion = DateTime.Now;

                // No actualizar contraseña en edición regular
                if (string.IsNullOrEmpty(persona.Contraseña))
                {
                    persona.Contraseña = existingPersona.Contraseña;
                }

                context.Entry(persona).State = EntityState.Modified;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int idPersona)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var persona = await context.Persona
                    .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

                if (persona == null) return false;

                // Soft delete - marcar como inactivo
                persona.Activo = false;
                persona.FechaModificacion = DateTime.Now;

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Persona>> GetAllAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .Include(p => p.IdDireccionNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<Persona?> GetByIdAsync(int idPersona)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .Include(p => p.IdDireccionNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPersona == idPersona);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Persona>> GetActiveAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .Where(p => p.Activo == true)
                    .Include(p => p.IdDireccionNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<List<Persona>> GetByRolAsync(int idRol)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .Where(p => p.IdRool == idRol && p.Activo == true)
                    .Include(p => p.IdRoolNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByRolAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<List<Persona>> GetBySucursalAsync(int idSucursal)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .Where(p => p.IdSucursal == idSucursal && p.Activo == true)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBySucursalAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Email == email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByEmailAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByDpiAsync(int dpi)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Dpi == dpi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByDpiAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<Persona?> GetByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return null;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Email == email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByEmailAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Persona>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                    return await GetActiveAsync();

                using var context = await _contextFactory.CreateDbContextAsync();

                var searchLower = searchTerm.ToLower();
                return await context.Persona
                    .Where(p => p.Activo == true &&
                               (EF.Functions.Like(p.Nombre.ToLower(), $"%{searchLower}%") ||
                                EF.Functions.Like(p.Apellido.ToLower(), $"%{searchLower}%") ||
                                EF.Functions.Like(p.Email.ToLower(), $"%{searchLower}%")))
                    .Include(p => p.IdDireccionNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        // Métodos adicionales para validaciones específicas en edición
        public async Task<bool> ExistsByEmailExcludingIdAsync(string email, int excludeId)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Email == email && p.IdPersona != excludeId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByEmailExcludingIdAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByDpiExcludingIdAsync(int dpi, int excludeId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Dpi == dpi && p.IdPersona != excludeId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByDpiExcludingIdAsync: {ex.Message}");
                return false;
            }
        }

        // Método para cambiar contraseña (por separado por seguridad)
        public async Task<bool> ChangePasswordAsync(int idPersona, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                var persona = await context.Persona
                    .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

                if (persona == null) return false;

                // TODO: En producción, implementar hash de contraseña
                // persona.Contraseña = BCrypt.Net.BCrypt.HashPassword(newPassword);
                persona.Contraseña = newPassword;
                persona.FechaModificacion = DateTime.Now;

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChangePasswordAsync: {ex.Message}");
                return false;
            }
        }

        // Cache para datos de referencia que no cambian frecuentemente
        private static List<Rol>? _rolesCache;
        private static List<Sucursal>? _sucursalesCache;
        private static List<Genero>? _generosCache;
        private static List<EstadoCivil>? _estadosCivilesCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        public async Task<List<Rol>> GetRolesAsync()
        {
            try
            {
                if (_rolesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _rolesCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _rolesCache = await context.Rol
                    .Where(r => r.Activo == true)
                    .OrderBy(r => r.TipoRol)
                    .AsNoTracking()
                    .ToListAsync();

                _lastCacheUpdate = DateTime.Now;
                return _rolesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRolesAsync: {ex.Message}");
                return new List<Rol>();
            }
        }

        public async Task<List<Sucursal>> GetSucursalesAsync()
        {
            try
            {
                if (_sucursalesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _sucursalesCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _sucursalesCache = await context.Sucursal
                    .Where(s => s.Activo == true)
                    .OrderBy(s => s.NombreSucursal)
                    .AsNoTracking()
                    .ToListAsync();

                return _sucursalesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSucursalesAsync: {ex.Message}");
                return new List<Sucursal>();
            }
        }

        public async Task<List<Genero>> GetGenerosAsync()
        {
            try
            {
                if (_generosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _generosCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _generosCache = await context.Genero
                    .Where(g => g.Activo == true)
                    .OrderBy(g => g.Ngenero)
                    .AsNoTracking()
                    .ToListAsync();

                return _generosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGenerosAsync: {ex.Message}");
                return new List<Genero>();
            }
        }

        public async Task<List<EstadoCivil>> GetEstadosCivilesAsync()
        {
            try
            {
                if (_estadosCivilesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _estadosCivilesCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _estadosCivilesCache = await context.EstadoCivil
                    .Where(e => e.Activo == true)
                    .OrderBy(e => e.IdEstadoCivil)
                    .AsNoTracking()
                    .ToListAsync();

                return _estadosCivilesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEstadosCivilesAsync: {ex.Message}");
                return new List<EstadoCivil>();
            }
        }

        // Método para limpiar cache manualmente si es necesario
        public static void ClearCache()
        {
            _rolesCache = null;
            _sucursalesCache = null;
            _generosCache = null;
            _estadosCivilesCache = null;
            _lastCacheUpdate = DateTime.MinValue;
        }
    }
}