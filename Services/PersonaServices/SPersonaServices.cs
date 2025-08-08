using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.PersonaServices;

namespace ProyectoFarmaVita.Services.PersonaServices
{
    public class SPersonaServices : IPersonaService
    {
        private readonly IDbContextFactory<FarmaDbContext> _contextFactory;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private static List<Departamento>? _departamentosCache;
        private static List<Municipio>? _municipiosCache;

        public SPersonaServices(IDbContextFactory<FarmaDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        #region Métodos Principales CRUD

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
            // Control de concurrencia para evitar múltiples operaciones simultáneas
            await _semaphore.WaitAsync();

            try
            {
                // Crear un contexto dedicado para esta operación
                using var context = await _contextFactory.CreateDbContextAsync();

                // Query base con todas las navegaciones necesarias
                var query = context.Persona
                    .Include(p => p.IdDireccionNavigation)
                        .ThenInclude(d => d != null ? d.IdMunicipioNavigation : null)
                            .ThenInclude(m => m != null ? m.IdDepartamentoNavigation : null)
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
                        (p.Nombre != null && EF.Functions.Like(p.Nombre.ToLower(), $"%{searchLower}%")) ||
                        (p.Apellido != null && EF.Functions.Like(p.Apellido.ToLower(), $"%{searchLower}%")) ||
                        (p.Email != null && EF.Functions.Like(p.Email.ToLower(), $"%{searchLower}%")) ||
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
                    _ => sortAscending ?
                        query.OrderBy(p => p.Nombre).ThenBy(p => p.Apellido) :
                        query.OrderByDescending(p => p.Nombre).ThenByDescending(p => p.Apellido)
                };

                // Obtener el total primero (con NoTracking para mejor rendimiento)
                var totalCount = await query.AsNoTracking().CountAsync();

                // Obtener los datos paginados
                var personas = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                // Log para debugging
                Console.WriteLine($"GetPaginatedAsync - Página: {pageNumber}, Tamaño: {pageSize}");
                Console.WriteLine($"GetPaginatedAsync - Retornando {personas.Count} personas de {totalCount} total");
                Console.WriteLine($"GetPaginatedAsync - Búsqueda: '{searchTerm}', Inactivos: {mostrarInactivos}");

                return (personas, totalCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPaginatedAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (new List<Persona>(), 0);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> AddAsync(Persona persona)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (persona == null)
                {
                    Console.WriteLine("AddAsync - Persona es null");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(persona.Nombre))
                {
                    Console.WriteLine("AddAsync - Nombre es requerido");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(persona.Email))
                {
                    Console.WriteLine("AddAsync - Email es requerido");
                    return false;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                // Verificar email único
                var emailExists = await context.Persona.AsNoTracking().AnyAsync(p => p.Email == persona.Email);
                if (emailExists)
                {
                    Console.WriteLine($"AddAsync - Email ya existe: {persona.Email}");
                    return false;
                }

                // Verificar DPI único si se proporciona
                if (persona.Dpi.HasValue)
                {
                    var dpiExists = await context.Persona.AsNoTracking().AnyAsync(p => p.Dpi == persona.Dpi.Value);
                    if (dpiExists)
                    {
                        Console.WriteLine($"AddAsync - DPI ya existe: {persona.Dpi}");
                        return false;
                    }
                }

                // Configurar campos automáticos
                persona.FechaCreacion = DateTime.Now;
                persona.FechaRegistro = DateTime.Now.ToString("yyyy-MM-dd");
                persona.Activo = persona.Activo ?? true;
                persona.UsuarioCreacion = "Sistema";

                context.Persona.Add(persona);
                var result = await context.SaveChangesAsync();

                Console.WriteLine($"AddAsync - Persona agregada: {result > 0}, ID: {persona.IdPersona}");

                // Limpiar cache después de agregar
                if (result > 0)
                {
                    ClearCache();
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(Persona persona)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (persona == null)
                {
                    Console.WriteLine("UpdateAsync - Persona es null");
                    return false;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var existingPersona = await context.Persona
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPersona == persona.IdPersona);

                if (existingPersona == null)
                {
                    Console.WriteLine($"UpdateAsync - Persona no encontrada: {persona.IdPersona}");
                    return false;
                }

                // Verificar email único (excluyendo la persona actual)
                if (!string.IsNullOrEmpty(persona.Email))
                {
                    var emailExists = await context.Persona
                        .AsNoTracking()
                        .AnyAsync(p => p.Email == persona.Email && p.IdPersona != persona.IdPersona);

                    if (emailExists)
                    {
                        Console.WriteLine($"UpdateAsync - Email ya existe: {persona.Email}");
                        return false;
                    }
                }

                // Verificar DPI único (excluyendo la persona actual)
                if (persona.Dpi.HasValue)
                {
                    var dpiExists = await context.Persona
                        .AsNoTracking()
                        .AnyAsync(p => p.Dpi == persona.Dpi.Value && p.IdPersona != persona.IdPersona);

                    if (dpiExists)
                    {
                        Console.WriteLine($"UpdateAsync - DPI ya existe: {persona.Dpi}");
                        return false;
                    }
                }

                // Conservar datos importantes del registro original
                persona.FechaCreacion = existingPersona.FechaCreacion;
                persona.UsuarioCreacion = existingPersona.UsuarioCreacion;
                persona.FechaRegistro = existingPersona.FechaRegistro;

                // Actualizar fecha de modificación
                persona.FechaModificacion = DateTime.Now;
                persona.UsuarioModificacion = "Sistema";

                // No actualizar contraseña en edición regular si está vacía
                if (string.IsNullOrEmpty(persona.Contraseña))
                {
                    persona.Contraseña = existingPersona.Contraseña;
                }

                // Usar Update en lugar de Entry para mejor tracking
                context.Persona.Update(persona);
                var result = await context.SaveChangesAsync();

                Console.WriteLine($"UpdateAsync - Persona actualizada: {result > 0}, ID: {persona.IdPersona}");

                // Limpiar cache después de actualizar
                if (result > 0)
                {
                    ClearCache();
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteAsync(int idPersona)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var persona = await context.Persona
                    .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

                if (persona == null)
                {
                    Console.WriteLine($"DeleteAsync - Persona no encontrada: {idPersona}");
                    return false;
                }

                // Soft delete - marcar como inactivo
                persona.Activo = false;
                persona.FechaModificacion = DateTime.Now;
                persona.UsuarioModificacion = "Sistema";

                var result = await context.SaveChangesAsync();
                Console.WriteLine($"DeleteAsync - Persona eliminada (soft delete): {result > 0}, ID: {idPersona}");

                // Limpiar cache después de eliminar
                if (result > 0)
                {
                    ClearCache();
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #endregion

        #region Métodos de Consulta

        public async Task<List<Persona>> GetAllAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var personas = await context.Persona
                    .Include(p => p.IdDireccionNavigation)
                        .ThenInclude(d => d != null ? d.IdMunicipioNavigation : null)
                            .ThenInclude(m => m != null ? m.IdDepartamentoNavigation : null)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.Nombre)
                    .ThenBy(p => p.Apellido)
                    .ToListAsync();

                Console.WriteLine($"GetAllAsync - Retornando {personas.Count} personas");
                return personas;
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

                var persona = await context.Persona
                    .Include(p => p.IdDireccionNavigation)
                        .ThenInclude(d => d != null ? d.IdMunicipioNavigation : null)
                            .ThenInclude(m => m != null ? m.IdDepartamentoNavigation : null)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

                Console.WriteLine($"GetByIdAsync - Persona encontrada: {persona != null}, ID: {idPersona}");
                return persona;
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

                var personas = await context.Persona
                    .Where(p => p.Activo == true)
                    .Include(p => p.IdDireccionNavigation)
                        .ThenInclude(d => d != null ? d.IdMunicipioNavigation : null)
                            .ThenInclude(m => m != null ? m.IdDepartamentoNavigation : null)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.Nombre)
                    .ThenBy(p => p.Apellido)
                    .ToListAsync();

                Console.WriteLine($"GetActiveAsync - Retornando {personas.Count} personas activas");
                return personas;
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

                var personas = await context.Persona
                    .Where(p => p.IdRool == idRol && p.Activo == true)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.Nombre)
                    .ThenBy(p => p.Apellido)
                    .ToListAsync();

                Console.WriteLine($"GetByRolAsync - Retornando {personas.Count} personas con rol {idRol}");
                return personas;
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

                var personas = await context.Persona
                    .Where(p => p.IdSucursal == idSucursal && p.Activo == true)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.Nombre)
                    .ThenBy(p => p.Apellido)
                    .ToListAsync();

                Console.WriteLine($"GetBySucursalAsync - Retornando {personas.Count} personas en sucursal {idSucursal}");
                return personas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBySucursalAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<List<Persona>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    Console.WriteLine("SearchAsync - Término de búsqueda vacío, retornando personas activas");
                    return await GetActiveAsync();
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var searchLower = searchTerm.ToLower();
                var personas = await context.Persona
                    .Where(p => p.Activo == true &&
                               ((p.Nombre != null && EF.Functions.Like(p.Nombre.ToLower(), $"%{searchLower}%")) ||
                                (p.Apellido != null && EF.Functions.Like(p.Apellido.ToLower(), $"%{searchLower}%")) ||
                                (p.Email != null && EF.Functions.Like(p.Email.ToLower(), $"%{searchLower}%"))))
                    .Include(p => p.IdDireccionNavigation)
                        .ThenInclude(d => d != null ? d.IdMunicipioNavigation : null)
                            .ThenInclude(m => m != null ? m.IdDepartamentoNavigation : null)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.Nombre)
                    .ThenBy(p => p.Apellido)
                    .ToListAsync();

                Console.WriteLine($"SearchAsync - Encontradas {personas.Count} personas con término '{searchTerm}'");
                return personas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        #endregion

        #region Métodos de Validación

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                var exists = await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Email == email);

                Console.WriteLine($"ExistsByEmailAsync - Email '{email}' existe: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByEmailAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByDpiAsync(long dpi)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var exists = await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Dpi == dpi);

                Console.WriteLine($"ExistsByDpiAsync - DPI '{dpi}' existe: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByDpiAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByEmailExcludingIdAsync(string email, int excludeId)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                var exists = await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Email == email && p.IdPersona != excludeId);

                Console.WriteLine($"ExistsByEmailExcludingIdAsync - Email '{email}' existe (excluyendo {excludeId}): {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByEmailExcludingIdAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByDpiExcludingIdAsync(long dpi, int excludeId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var exists = await context.Persona
                    .AsNoTracking()
                    .AnyAsync(p => p.Dpi == dpi && p.IdPersona != excludeId);

                Console.WriteLine($"ExistsByDpiExcludingIdAsync - DPI '{dpi}' existe (excluyendo {excludeId}): {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByDpiExcludingIdAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<Persona?> GetByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return null;

                using var context = await _contextFactory.CreateDbContextAsync();

                var persona = await context.Persona
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Email == email);

                Console.WriteLine($"GetByEmailAsync - Persona encontrada por email '{email}': {persona != null}");
                return persona;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByEmailAsync: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos de Seguridad

        public async Task<bool> ChangePasswordAsync(int idPersona, string newPassword)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    Console.WriteLine("ChangePasswordAsync - Contraseña inválida");
                    return false;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var persona = await context.Persona
                    .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

                if (persona == null)
                {
                    Console.WriteLine($"ChangePasswordAsync - Persona no encontrada: {idPersona}");
                    return false;
                }

                // TODO: En producción, implementar hash de contraseña
                // persona.Contraseña = BCrypt.Net.BCrypt.HashPassword(newPassword);
                persona.Contraseña = newPassword;
                persona.FechaModificacion = DateTime.Now;
                persona.UsuarioModificacion = "Sistema";

                var result = await context.SaveChangesAsync();
                Console.WriteLine($"ChangePasswordAsync - Contraseña cambiada: {result > 0}, ID: {idPersona}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChangePasswordAsync: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #endregion

        #region Cache y Datos de Referencia

        // Cache para datos de referencia que no cambian frecuentemente
        private static List<Rol>? _rolesCache;
        private static List<Sucursal>? _sucursalesCache;
        private static List<Genero>? _generosCache;
        private static List<EstadoCivil>? _estadosCivilesCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);
        private static readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

        public async Task<List<Rol>> GetRolesAsync()
        {
            await _cacheSemaphore.WaitAsync();

            try
            {
                if (_rolesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                {
                    Console.WriteLine("GetRolesAsync - Retornando roles desde cache");
                    return _rolesCache;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                _rolesCache = await context.Rol
                    .Where(r => r.Activo == true)
                    .OrderBy(r => r.TipoRol)
                    .AsNoTracking()
                    .ToListAsync();

                _lastCacheUpdate = DateTime.Now;
                Console.WriteLine($"GetRolesAsync - Roles cargados desde BD: {_rolesCache.Count}");
                return _rolesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRolesAsync: {ex.Message}");
                return new List<Rol>();
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        public async Task<List<Sucursal>> GetSucursalesAsync()
        {
            await _cacheSemaphore.WaitAsync();

            try
            {
                if (_sucursalesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                {
                    Console.WriteLine("GetSucursalesAsync - Retornando sucursales desde cache");
                    return _sucursalesCache;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                _sucursalesCache = await context.Sucursal
                    .Where(s => s.Activo == true)
                    .OrderBy(s => s.NombreSucursal)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"GetSucursalesAsync - Sucursales cargadas desde BD: {_sucursalesCache.Count}");
                return _sucursalesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSucursalesAsync: {ex.Message}");
                return new List<Sucursal>();
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        public async Task<List<Genero>> GetGenerosAsync()
        {
            await _cacheSemaphore.WaitAsync();

            try
            {
                if (_generosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                {
                    Console.WriteLine("GetGenerosAsync - Retornando géneros desde cache");
                    return _generosCache;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                _generosCache = await context.Genero
                    .Where(g => g.Activo == true)
                    .OrderBy(g => g.Ngenero)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"GetGenerosAsync - Géneros cargados desde BD: {_generosCache.Count}");
                return _generosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGenerosAsync: {ex.Message}");
                return new List<Genero>();
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        public async Task<List<EstadoCivil>> GetEstadosCivilesAsync()
        {
            await _cacheSemaphore.WaitAsync();

            try
            {
                if (_estadosCivilesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                {
                    Console.WriteLine("GetEstadosCivilesAsync - Retornando estados civiles desde cache");
                    return _estadosCivilesCache;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                _estadosCivilesCache = await context.EstadoCivil
                    .Where(e => e.Activo == true)
                    .OrderBy(e => e.IdEstadoCivil)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"GetEstadosCivilesAsync - Estados civiles cargados desde BD: {_estadosCivilesCache.Count}");
                return _estadosCivilesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEstadosCivilesAsync: {ex.Message}");
                return new List<EstadoCivil>();
            }
            finally
            {
                _cacheSemaphore.Release();
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
            Console.WriteLine("SPersonaServices - Cache limpiado");
        }

        #endregion

        #region Métodos de Utilidad y Estadísticas

        public async Task<int> GetTotalPersonasAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var total = await context.Persona.AsNoTracking().CountAsync();
                Console.WriteLine($"GetTotalPersonasAsync - Total personas: {total}");
                return total;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTotalPersonasAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetTotalPersonasActivasAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var total = await context.Persona.AsNoTracking().CountAsync(p => p.Activo == true);
                Console.WriteLine($"GetTotalPersonasActivasAsync - Total personas activas: {total}");
                return total;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTotalPersonasActivasAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetPersonasPorRolAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var estadisticas = await context.Persona
                    .Where(p => p.Activo == true)
                    .Include(p => p.IdRoolNavigation)
                    .AsNoTracking()
                    .GroupBy(p => p.IdRoolNavigation.TipoRol ?? "Sin Rol")
                    .Select(g => new { Rol = g.Key, Cantidad = g.Count() })
                    .ToDictionaryAsync(x => x.Rol, x => x.Cantidad);

                Console.WriteLine($"GetPersonasPorRolAsync - Estadísticas por rol generadas: {estadisticas.Count} roles");
                return estadisticas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPersonasPorRolAsync: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        public async Task<List<Persona>> GetPersonasRecientesAsync(int cantidad = 10)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var personas = await context.Persona
                    .Where(p => p.Activo == true)
                    .Include(p => p.IdRoolNavigation)
                    .Include(p => p.IdEstadoCivilNavigation)
                    .Include(p => p.IdGeneroNavigation)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Take(cantidad)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"GetPersonasRecientesAsync - Retornando {personas.Count} personas recientes");
                return personas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPersonasRecientesAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<int?> CreateTelefonoAsync(int numeroTelefonico)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verificar si ya existe ese número
                var telefonoExistente = await context.Telefono
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.NumeroTelefonico == numeroTelefonico);

                if (telefonoExistente != null)
                {
                    Console.WriteLine($"CreateTelefonoAsync - Teléfono ya existe: {numeroTelefonico}, ID: {telefonoExistente.IdTelefono}");
                    return telefonoExistente.IdTelefono;
                }

                var nuevoTelefono = new Telefono
                {
                    NumeroTelefonico = numeroTelefonico,
                    Activo = true
                };

                context.Telefono.Add(nuevoTelefono);
                var result = await context.SaveChangesAsync();

                if (result > 0)
                {
                    Console.WriteLine($"CreateTelefonoAsync - Teléfono creado: {numeroTelefonico}, ID: {nuevoTelefono.IdTelefono}");
                    return nuevoTelefono.IdTelefono;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateTelefonoAsync: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        public async Task<int?> UpdateTelefonoAsync(int idTelefono, int numeroTelefonico)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var telefono = await context.Telefono
                    .FirstOrDefaultAsync(t => t.IdTelefono == idTelefono);

                if (telefono == null)
                {
                    Console.WriteLine($"UpdateTelefonoAsync - Teléfono no encontrado: {idTelefono}");
                    // Si no existe, crear uno nuevo
                    return await CreateTelefonoAsync(numeroTelefonico);
                }

                telefono.NumeroTelefonico = numeroTelefonico;
                var result = await context.SaveChangesAsync();

                if (result > 0)
                {
                    Console.WriteLine($"UpdateTelefonoAsync - Teléfono actualizado: {numeroTelefonico}, ID: {idTelefono}");
                    return idTelefono;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTelefonoAsync: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Telefono?> GetTelefonoByIdAsync(int idTelefono)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Telefono
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdTelefono == idTelefono);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTelefonoByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<int?> CreateDireccionAsync(string direccion, int idMunicipio)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var nuevaDireccion = new Direccion
                {
                    Direccion1 = direccion,
                    IdMunicipio = idMunicipio,
                    Activo = true
                };

                context.Direccion.Add(nuevaDireccion);
                var result = await context.SaveChangesAsync();

                if (result > 0)
                {
                    Console.WriteLine($"CreateDireccionAsync - Dirección creada: {direccion}, ID: {nuevaDireccion.IdDireccion}");
                    return nuevaDireccion.IdDireccion;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateDireccionAsync: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int?> UpdateDireccionAsync(int idDireccion, string direccion, int idMunicipio)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var direccionExistente = await context.Direccion
                    .FirstOrDefaultAsync(d => d.IdDireccion == idDireccion);

                if (direccionExistente == null)
                {
                    Console.WriteLine($"UpdateDireccionAsync - Dirección no encontrada: {idDireccion}");
                    // Si no existe, crear una nueva
                    return await CreateDireccionAsync(direccion, idMunicipio);
                }

                direccionExistente.Direccion1 = direccion;
                direccionExistente.IdMunicipio = idMunicipio;
                var result = await context.SaveChangesAsync();

                if (result > 0)
                {
                    Console.WriteLine($"UpdateDireccionAsync - Dirección actualizada: {direccion}, ID: {idDireccion}");
                    return idDireccion;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateDireccionAsync: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Direccion?> GetDireccionByIdAsync(int idDireccion)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Direccion
                    .Include(d => d.IdMunicipioNavigation)
                        .ThenInclude(m => m != null ? m.IdDepartamentoNavigation : null)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.IdDireccion == idDireccion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDireccionByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            await _cacheSemaphore.WaitAsync();

            try
            {
                // Usar cache para departamentos también
                if (_departamentosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                {
                    Console.WriteLine("GetDepartamentosAsync - Retornando departamentos desde cache");
                    return _departamentosCache;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                _departamentosCache = await context.Departamento
                    .OrderBy(d => d.NombreDepartamento)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"GetDepartamentosAsync - Departamentos cargados desde BD: {_departamentosCache.Count}");
                return _departamentosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartamentosAsync: {ex.Message}");
                return new List<Departamento>();
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        public async Task<List<Municipio>> GetMunicipiosAsync()
        {
            await _cacheSemaphore.WaitAsync();

            try
            {
                // Usar cache para municipios también
                if (_municipiosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                {
                    Console.WriteLine("GetMunicipiosAsync - Retornando municipios desde cache");
                    return _municipiosCache;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                _municipiosCache = await context.Municipio
                    .Include(m => m.IdDepartamentoNavigation)
                    .OrderBy(m => m.NombreMunicipio)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"GetMunicipiosAsync - Municipios cargados desde BD: {_municipiosCache.Count}");
                return _municipiosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMunicipiosAsync: {ex.Message}");
                return new List<Municipio>();
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }



        #endregion

        #region IDisposable

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        #endregion
    }



}
            