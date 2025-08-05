using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.SucursalServices;

namespace ProyectoFarmaVita.Services.SucursalServices
{
    public class SSucursalServices : ISucursalService
    {
        private readonly IDbContextFactory<FarmaDbContext> _contextFactory;

        public SSucursalServices(IDbContextFactory<FarmaDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<(List<Sucursal> sucursales, int totalCount)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            string searchTerm = "",
            bool sortAscending = true,
            string sortBy = "NombreSucursal",
            bool mostrarInactivos = false,
            bool mostrarTodos = false,
            int? responsableId = null,
            int? inventarioId = null,
            int? departamentoId = null,
            int? municipioId = null)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.Sucursal
                    .Include(s => s.ResponsableSucursalNavigation)
                    .Include(s => s.IdDireccionNavigation)
                        .ThenInclude(d => d.IdMunicipioNavigation)
                            .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(s => s.IdTelefonoNavigation)
                    .Include(s => s.IdInventarioNavigation)
                    .AsQueryable();

                // Filtro por estado activo/inactivo
                if (!mostrarTodos)
                {
                    if (mostrarInactivos)
                    {
                        query = query.Where(s => s.Activo == false);
                    }
                    else
                    {
                        query = query.Where(s => s.Activo == true);
                    }
                }
                // Si mostrarTodos es true, no aplicamos filtro de estado

                // Aplicar búsqueda
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    query = query.Where(s =>
                        EF.Functions.Like(s.NombreSucursal.ToLower(), $"%{searchLower}%") ||
                        EF.Functions.Like(s.EmailSucursal.ToLower(), $"%{searchLower}%"));
                }

                // Aplicar filtros adicionales
                if (responsableId.HasValue)
                    query = query.Where(s => s.ResponsableSucursal == responsableId.Value);

                if (inventarioId.HasValue)
                    query = query.Where(s => s.IdInventario == inventarioId.Value);

                if (departamentoId.HasValue)
                    query = query.Where(s => s.IdDireccionNavigation.IdMunicipioNavigation.IdDepartamento == departamentoId.Value);

                if (municipioId.HasValue)
                    query = query.Where(s => s.IdDireccionNavigation.IdMunicipio == municipioId.Value);

                // Aplicar ordenamiento
                query = sortBy.ToLower() switch
                {
                    "nombresucursal" => sortAscending ? query.OrderBy(s => s.NombreSucursal) : query.OrderByDescending(s => s.NombreSucursal),
                    "emailsucursal" => sortAscending ? query.OrderBy(s => s.EmailSucursal) : query.OrderByDescending(s => s.EmailSucursal),
                    "idsucursal" => sortAscending ? query.OrderBy(s => s.IdSucursal) : query.OrderByDescending(s => s.IdSucursal),
                    _ => sortAscending ? query.OrderBy(s => s.NombreSucursal) : query.OrderByDescending(s => s.NombreSucursal)
                };

                // Ejecutar consultas de forma paralela para mejor rendimiento
                var countTask = query.CountAsync();
                var dataTask = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                await Task.WhenAll(countTask, dataTask);

                return (await dataTask, await countTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPaginatedAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (new List<Sucursal>(), 0);
            }
        }

        public async Task<bool> AddAsync(Sucursal sucursal)
        {
            try
            {
                if (sucursal == null) return false;
                if (string.IsNullOrEmpty(sucursal.NombreSucursal)) return false;
                if (string.IsNullOrEmpty(sucursal.EmailSucursal)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                // Verificar existencia de forma paralela
                var emailExistsTask = context.Sucursal.AnyAsync(s => s.EmailSucursal == sucursal.EmailSucursal);
                var nombreExistsTask = context.Sucursal.AnyAsync(s => s.NombreSucursal == sucursal.NombreSucursal);

                await Task.WhenAll(emailExistsTask, nombreExistsTask);

                if (await emailExistsTask) return false;
                if (await nombreExistsTask) return false;

                // Configurar campos automáticos
                sucursal.Activo = sucursal.Activo ?? true;

                context.Sucursal.Add(sucursal);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Sucursal sucursal)
        {
            try
            {
                if (sucursal == null) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                var existingSucursal = await context.Sucursal
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IdSucursal == sucursal.IdSucursal);

                if (existingSucursal == null) return false;

                // Verificar email único (excluyendo la sucursal actual)
                if (!string.IsNullOrEmpty(sucursal.EmailSucursal))
                {
                    var emailExists = await context.Sucursal
                        .AsNoTracking()
                        .AnyAsync(s => s.EmailSucursal == sucursal.EmailSucursal && s.IdSucursal != sucursal.IdSucursal);

                    if (emailExists) return false;
                }

                // Verificar nombre único (excluyendo la sucursal actual)
                if (!string.IsNullOrEmpty(sucursal.NombreSucursal))
                {
                    var nombreExists = await context.Sucursal
                        .AsNoTracking()
                        .AnyAsync(s => s.NombreSucursal == sucursal.NombreSucursal && s.IdSucursal != sucursal.IdSucursal);

                    if (nombreExists) return false;
                }

                context.Entry(sucursal).State = EntityState.Modified;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int idSucursal)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var sucursal = await context.Sucursal
                    .FirstOrDefaultAsync(s => s.IdSucursal == idSucursal);

                if (sucursal == null) return false;

                // Soft delete - marcar como inactivo
                sucursal.Activo = false;

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Sucursal>> GetAllAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .Include(s => s.ResponsableSucursalNavigation)
                    .Include(s => s.IdDireccionNavigation)
                        .ThenInclude(d => d.IdMunicipioNavigation)
                            .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(s => s.IdTelefonoNavigation)
                    .Include(s => s.IdInventarioNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<Sucursal>();
            }
        }

        public async Task<Sucursal?> GetByIdAsync(int idSucursal)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .Include(s => s.ResponsableSucursalNavigation)
                    .Include(s => s.IdDireccionNavigation)
                        .ThenInclude(d => d.IdMunicipioNavigation)
                            .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(s => s.IdTelefonoNavigation)
                    .Include(s => s.IdInventarioNavigation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IdSucursal == idSucursal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Sucursal>> GetActiveAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .Where(s => s.Activo == true)
                    .Include(s => s.ResponsableSucursalNavigation)
                    .Include(s => s.IdDireccionNavigation)
                        .ThenInclude(d => d.IdMunicipioNavigation)
                            .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(s => s.IdTelefonoNavigation)
                    .Include(s => s.IdInventarioNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveAsync: {ex.Message}");
                return new List<Sucursal>();
            }
        }

        public async Task<List<Sucursal>> GetByResponsableAsync(int idResponsable)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .Where(s => s.ResponsableSucursal == idResponsable && s.Activo == true)
                    .Include(s => s.ResponsableSucursalNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByResponsableAsync: {ex.Message}");
                return new List<Sucursal>();
            }
        }

        public async Task<List<Sucursal>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                    return await GetActiveAsync();

                using var context = await _contextFactory.CreateDbContextAsync();

                var searchLower = searchTerm.ToLower();
                return await context.Sucursal
                    .Where(s => s.Activo == true &&
                               (EF.Functions.Like(s.NombreSucursal.ToLower(), $"%{searchLower}%") ||
                                EF.Functions.Like(s.EmailSucursal.ToLower(), $"%{searchLower}%")))
                    .Include(s => s.ResponsableSucursalNavigation)
                    .Include(s => s.IdDireccionNavigation)
                        .ThenInclude(d => d.IdMunicipioNavigation)
                            .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(s => s.IdTelefonoNavigation)
                    .Include(s => s.IdInventarioNavigation)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchAsync: {ex.Message}");
                return new List<Sucursal>();
            }
        }

        // Métodos de validación
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .AsNoTracking()
                    .AnyAsync(s => s.EmailSucursal == email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByEmailAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByNombreAsync(string nombre)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .AsNoTracking()
                    .AnyAsync(s => s.NombreSucursal == nombre);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByNombreAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByEmailExcludingIdAsync(string email, int excludeId)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .AsNoTracking()
                    .AnyAsync(s => s.EmailSucursal == email && s.IdSucursal != excludeId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByEmailExcludingIdAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsByNombreExcludingIdAsync(string nombre, int excludeId)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre)) return false;

                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sucursal
                    .AsNoTracking()
                    .AnyAsync(s => s.NombreSucursal == nombre && s.IdSucursal != excludeId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExistsByNombreExcludingIdAsync: {ex.Message}");
                return false;
            }
        }

        // Cache para datos de referencia
        private static List<Persona>? _personasCache;
        private static List<Inventario>? _inventariosCache;
        private static List<Telefono>? _telefonosCache;
        private static List<Direccion>? _direccionesCache;
        private static List<Departamento>? _departamentosCache;
        private static List<Municipio>? _municipiosCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        public async Task<List<Persona>> GetPersonasAsync()
        {
            try
            {
                if (_personasCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _personasCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _personasCache = await context.Persona
                    .Where(p => p.Activo == true)
                    .OrderBy(p => p.Nombre)
                    .ThenBy(p => p.Apellido)
                    .AsNoTracking()
                    .ToListAsync();

                _lastCacheUpdate = DateTime.Now;
                return _personasCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPersonasAsync: {ex.Message}");
                return new List<Persona>();
            }
        }

        public async Task<List<Inventario>> GetInventariosAsync()
        {
            try
            {
                if (_inventariosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _inventariosCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _inventariosCache = await context.Inventario
                    .OrderBy(i => i.NombreInventario)
                    .AsNoTracking()
                    .ToListAsync();

                return _inventariosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetInventariosAsync: {ex.Message}");
                return new List<Inventario>();
            }
        }

        public async Task<List<Telefono>> GetTelefonosAsync()
        {
            try
            {
                if (_telefonosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _telefonosCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _telefonosCache = await context.Telefono
                    .Where(t => t.Activo == true)
                    .AsNoTracking()
                    .ToListAsync();

                return _telefonosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTelefonosAsync: {ex.Message}");
                return new List<Telefono>();
            }
        }

        public async Task<List<Direccion>> GetDireccionesAsync()
        {
            try
            {
                if (_direccionesCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _direccionesCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _direccionesCache = await context.Direccion
                    .Where(d => d.Activo == true)
                    .Include(d => d.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                    .AsNoTracking()
                    .ToListAsync();

                return _direccionesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDireccionesAsync: {ex.Message}");
                return new List<Direccion>();
            }
        }

        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            try
            {
                if (_departamentosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _departamentosCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _departamentosCache = await context.Departamento
                    .OrderBy(d => d.NombreDepartamento)
                    .AsNoTracking()
                    .ToListAsync();

                return _departamentosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartamentosAsync: {ex.Message}");
                return new List<Departamento>();
            }
        }

        public async Task<List<Municipio>> GetMunicipiosAsync()
        {
            try
            {
                if (_municipiosCache != null && DateTime.Now - _lastCacheUpdate < CacheExpiry)
                    return _municipiosCache;

                using var context = await _contextFactory.CreateDbContextAsync();

                _municipiosCache = await context.Municipio
                    .Include(m => m.IdDepartamentoNavigation)
                    .OrderBy(m => m.NombreMunicipio)
                    .AsNoTracking()
                    .ToListAsync();

                return _municipiosCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMunicipiosAsync: {ex.Message}");
                return new List<Municipio>();
            }
        }

        public async Task<List<Municipio>> GetMunicipiosByDepartamentoAsync(int idDepartamento)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Municipio
                    .Where(m => m.IdDepartamento == idDepartamento)
                    .Include(m => m.IdDepartamentoNavigation)
                    .OrderBy(m => m.NombreMunicipio)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMunicipiosByDepartamentoAsync: {ex.Message}");
                return new List<Municipio>();
            }
        }

        // Método para limpiar cache manualmente si es necesario
        public static void ClearCache()
        {
            _personasCache = null;
            _inventariosCache = null;
            _telefonosCache = null;
            _direccionesCache = null;
            _departamentosCache = null;
            _municipiosCache = null;
            _lastCacheUpdate = DateTime.MinValue;
        }
    }
}