using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.InventarioService
{
    public class SInventarioService : IInventarioService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SInventarioService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        #region Métodos CRUD Básicos

        public async Task<bool> AddUpdateAsync(Inventario inventario)
        {
            try
            {
                if (inventario.IdInventario > 0)
                {
                    // Buscar el inventario existente en la base de datos
                    var existingInventario = await _farmaDbContext.Inventario
                        .Include(i => i.IdProductoNavigation)
                        .FirstOrDefaultAsync(i => i.IdInventario == inventario.IdInventario);

                    if (existingInventario != null)
                    {
                        // Validar que no existe otro inventario con el mismo nombre para el mismo producto
                        var duplicateCheck = await _farmaDbContext.Inventario
                            .AnyAsync(i => i.IdProducto == inventario.IdProducto &&
                                         i.NombreInventario.ToLower() == inventario.NombreInventario.ToLower() &&
                                         i.IdInventario != inventario.IdInventario);

                        if (duplicateCheck)
                        {
                            throw new InvalidOperationException("Ya existe un inventario con este nombre para el producto seleccionado");
                        }

                        // Validar que stock mínimo sea menor que stock máximo
                        if (inventario.StockMinimo.HasValue && inventario.StockMaximo.HasValue
                            && inventario.StockMinimo >= inventario.StockMaximo)
                        {
                            throw new InvalidOperationException("El stock mínimo debe ser menor que el stock máximo");
                        }

                        // Actualizar las propiedades existentes
                        existingInventario.IdProducto = inventario.IdProducto;
                        existingInventario.NombreInventario = inventario.NombreInventario;
                        existingInventario.Cantidad = inventario.Cantidad;
                        existingInventario.StockMinimo = inventario.StockMinimo;
                        existingInventario.StockMaximo = inventario.StockMaximo;
                        existingInventario.UltimaActualizacion = DateTime.Now;

                        // Marcar el inventario como modificado
                        _farmaDbContext.Inventario.Update(existingInventario);
                    }
                    else
                    {
                        return false; // Si no se encontró el inventario, devolver false
                    }
                }
                else
                {
                    // Validar que no existe otro inventario con el mismo nombre para el mismo producto
                    var duplicateCheck = await _farmaDbContext.Inventario
                        .AnyAsync(i => i.IdProducto == inventario.IdProducto &&
                                     i.NombreInventario.ToLower() == inventario.NombreInventario.ToLower());

                    if (duplicateCheck)
                    {
                        throw new InvalidOperationException("Ya existe un inventario con este nombre para el producto seleccionado");
                    }

                    // Validar que stock mínimo sea menor que stock máximo
                    if (inventario.StockMinimo.HasValue && inventario.StockMaximo.HasValue
                        && inventario.StockMinimo >= inventario.StockMaximo)
                    {
                        throw new InvalidOperationException("El stock mínimo debe ser menor que el stock máximo");
                    }

                    inventario.UltimaActualizacion = DateTime.Now;

                    // Si no hay ID, se trata de un nuevo inventario, agregarlo
                    _farmaDbContext.Inventario.Add(inventario);
                }

                // Guardar los cambios en la base de datos
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en AddUpdateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id_inventario)
        {
            try
            {
                var inventario = await _farmaDbContext.Inventario
                    .Include(i => i.Sucursal) // Verificar si hay sucursales asociadas
                    .FirstOrDefaultAsync(i => i.IdInventario == id_inventario);

                if (inventario != null)
                {
                    // Verificar si el inventario está asociado a alguna sucursal
                    if (inventario.Sucursal != null && inventario.Sucursal.Any())
                    {
                        throw new InvalidOperationException($"No se puede eliminar el inventario porque está asociado a {inventario.Sucursal.Count} sucursal(es)");
                    }

                    // Eliminar físicamente el inventario si no tiene dependencias
                    _farmaDbContext.Inventario.Remove(inventario);
                    await _farmaDbContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en DeleteAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Inventario>> GetAllAsync()
        {
            try
            {
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .OrderBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Inventario> GetByIdAsync(int id_inventario)
        {
            try
            {
                var result = await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Include(i => i.Sucursal) // Para verificar dependencias
                    .FirstOrDefaultAsync(i => i.IdInventario == id_inventario);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el inventario con ID {id_inventario}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetByIdAsync: {ex.Message}");
                throw new Exception("Error al recuperar el inventario", ex);
            }
        }

        public async Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            try
            {
                var query = _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .AsQueryable();

                // Filtro por el término de búsqueda
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(i => i.NombreInventario.Contains(searchTerm) ||
                                           (i.IdProductoNavigation != null &&
                                            i.IdProductoNavigation.NombreProducto.Contains(searchTerm)));
                }

                // Ordenamiento basado en el campo NombreInventario
                query = sortAscending
                    ? query.OrderBy(i => i.NombreInventario).ThenBy(i => i.IdInventario)
                    : query.OrderByDescending(i => i.NombreInventario).ThenByDescending(i => i.IdInventario);

                var totalItems = await query.CountAsync();

                // Aplicar paginación
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new MPaginatedResult<Inventario>
                {
                    Items = items,
                    TotalCount = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetPaginatedAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Consultas por Producto

        public async Task<List<Inventario>> GetByProductoIdAsync(int productoId)
        {
            try
            {
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Where(i => i.IdProducto == productoId)
                    .OrderBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetByProductoIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExistsForProductAsync(int productoId, string nombreInventario, int? excludeId = null)
        {
            try
            {
                var query = _farmaDbContext.Inventario
                    .Where(i => i.IdProducto == productoId &&
                               i.NombreInventario.ToLower() == nombreInventario.ToLower());

                if (excludeId.HasValue)
                {
                    query = query.Where(i => i.IdInventario != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ExistsForProductAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Consultas por Estado de Stock

        public async Task<List<Inventario>> GetLowStockAsync()
        {
            try
            {
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Where(i => i.Cantidad <= i.StockMinimo)
                    .OrderBy(i => i.Cantidad)
                    .ThenBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetLowStockAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Inventario>> GetOutOfStockAsync()
        {
            try
            {
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Where(i => i.Cantidad == null || i.Cantidad == 0)
                    .OrderBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetOutOfStockAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Inventario>> GetHighStockAsync()
        {
            try
            {
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Where(i => i.Cantidad >= i.StockMaximo)
                    .OrderBy(i => i.Cantidad)
                    .ThenBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetHighStockAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Gestión de Stock

        public async Task<bool> AdjustStockAsync(int inventarioId, int adjustment, string reason = "")
        {
            try
            {
                var inventario = await _farmaDbContext.Inventario.FindAsync(inventarioId);

                if (inventario == null)
                {
                    return false;
                }

                var nuevaCantidad = (inventario.Cantidad ?? 0) + adjustment;

                if (nuevaCantidad < 0)
                {
                    throw new InvalidOperationException("La cantidad resultante no puede ser negativa");
                }

                inventario.Cantidad = nuevaCantidad;
                inventario.UltimaActualizacion = DateTime.Now;

                _farmaDbContext.Inventario.Update(inventario);
                await _farmaDbContext.SaveChangesAsync();

                // Log del ajuste
                Console.WriteLine($"Ajuste de stock realizado: Inventario {inventarioId}, Ajuste: {adjustment}, Nueva cantidad: {nuevaCantidad}. Motivo: {reason}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en AdjustStockAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SetStockAsync(int inventarioId, int newQuantity, string reason = "")
        {
            try
            {
                var inventario = await _farmaDbContext.Inventario.FindAsync(inventarioId);

                if (inventario == null)
                {
                    return false;
                }

                if (newQuantity < 0)
                {
                    throw new InvalidOperationException("La cantidad no puede ser negativa");
                }

                var cantidadAnterior = inventario.Cantidad ?? 0;
                inventario.Cantidad = newQuantity;
                inventario.UltimaActualizacion = DateTime.Now;

                _farmaDbContext.Inventario.Update(inventario);
                await _farmaDbContext.SaveChangesAsync();

                // Log del cambio
                Console.WriteLine($"Stock establecido: Inventario {inventarioId}, Cantidad anterior: {cantidadAnterior}, Nueva cantidad: {newQuantity}. Motivo: {reason}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en SetStockAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Consultas por Sucursal

        public async Task<List<Inventario>> GetInventariosBySucursalAsync(int sucursalId)
        {
            try
            {
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Include(i => i.Sucursal)
                    .Where(i => i.Sucursal.Any(s => s.IdSucursal == sucursalId))
                    .OrderBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetInventariosBySucursalAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Estadísticas y Reportes

        public async Task<Dictionary<string, int>> GetInventoryStatisticsAsync()
        {
            try
            {
                var inventarios = await _farmaDbContext.Inventario
                    .AsNoTracking()
                    .ToListAsync();

                var stats = new Dictionary<string, int>
                {
                    ["TotalInventarios"] = inventarios.Count,
                    ["TotalStock"] = inventarios.Sum(i => i.Cantidad ?? 0),
                    ["StockBajo"] = inventarios.Count(i => i.Cantidad <= i.StockMinimo),
                    ["SinStock"] = inventarios.Count(i => (i.Cantidad ?? 0) == 0),
                    ["StockAlto"] = inventarios.Count(i => i.Cantidad >= i.StockMaximo),
                    ["StockNormal"] = inventarios.Count(i =>
                        i.Cantidad > i.StockMinimo && i.Cantidad < i.StockMaximo)
                };

                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetInventoryStatisticsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Inventario>> GetInventariosExpiringSoonAsync(int days = 30)
        {
            try
            {
                var fechaLimite = DateTime.Now.AddDays(days);

                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Where(i => i.IdProductoNavigation != null &&
                               i.IdProductoNavigation.FechaVencimiento.HasValue &&
                               i.IdProductoNavigation.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) <= fechaLimite &&
                               (i.Cantidad ?? 0) > 0)
                    .OrderBy(i => i.IdProductoNavigation.FechaVencimiento)
                    .ThenBy(i => i.NombreInventario)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetInventariosExpiringSoonAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Inventario>> GetTopMovedInventariosAsync(int top = 10)
        {
            try
            {
                // Este método podría expandirse con una tabla de movimientos de inventario
                // Por ahora, devolvemos los inventarios con más stock
                return await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.IdProductoNavigation)
                        .ThenInclude(p => p.IdProveedorNavigation)
                    .Where(i => (i.Cantidad ?? 0) > 0)
                    .OrderByDescending(i => i.Cantidad)
                    .Take(top)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetTopMovedInventariosAsync: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}