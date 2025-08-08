using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.InventarioServices;

namespace ProyectoFarmaVita.Services.InventarioServices
{
    public class SInventarioService : IInventarioService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SInventarioService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Inventario inventario)
        {
            if (inventario.IdInventario > 0)
            {
                // Buscar el inventario existente en la base de datos
                var existingInventario = await _farmaDbContext.Inventario.FindAsync(inventario.IdInventario);

                if (existingInventario != null)
                {
                    // Actualizar las propiedades existentes
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
                inventario.UltimaActualizacion = DateTime.Now;
                inventario.Cantidad = inventario.Cantidad ?? 0;

                // Si no hay ID, se trata de un nuevo inventario, agregarlo
                _farmaDbContext.Inventario.Add(inventario);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_inventario)
        {
            var inventario = await _farmaDbContext.Inventario
                .Include(i => i.Sucursal)
                .Include(i => i.InventarioProducto)
                .FirstOrDefaultAsync(i => i.IdInventario == id_inventario);

            if (inventario != null)
            {
                // Verificar si el inventario tiene dependencias
                bool hasDependencies = (inventario.Sucursal != null && inventario.Sucursal.Any()) ||
                                     (inventario.InventarioProducto != null && inventario.InventarioProducto.Any());

                if (hasDependencies)
                {
                    return false; // No se puede eliminar si tiene dependencias
                }

                // Eliminar físicamente el inventario (no tiene campo Activo en el modelo actual)
                _farmaDbContext.Inventario.Remove(inventario);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Inventario>> GetAllAsync()
        {
            return await _farmaDbContext.Inventario
                .Include(i => i.InventarioProducto)
                    .ThenInclude(ip => ip.IdProductoNavigation)
                .OrderBy(i => i.NombreInventario)
                .ToListAsync();
        }

        public async Task<Inventario> GetByIdAsync(int id_inventario)
        {
            try
            {
                var result = await _farmaDbContext.Inventario
                    .Include(i => i.InventarioProducto)
                        .ThenInclude(ip => ip.IdProductoNavigation)
                            .ThenInclude(p => p.IdCategoriaNavigation)
                    .Include(i => i.InventarioProducto)
                        .ThenInclude(ip => ip.IdProductoNavigation)
                            .ThenInclude(p => p.IdProveedorNavigation)
                    .FirstOrDefaultAsync(i => i.IdInventario == id_inventario);

                if (result == null)
                {
                    throw new KeyNotFoundException($"No se encontró el inventario con ID {id_inventario}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el inventario", ex);
            }
        }

        public async Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Inventario
                .Include(i => i.InventarioProducto)
                    .ThenInclude(ip => ip.IdProductoNavigation)
                .AsQueryable();

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i => i.NombreInventario.Contains(searchTerm));
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

        public async Task<List<Inventario>> GetLowStockAsync()
        {
            return await _farmaDbContext.Inventario
                .Include(i => i.InventarioProducto)
                    .ThenInclude(ip => ip.IdProductoNavigation)
                .Where(i => i.Cantidad <= i.StockMinimo)
                .OrderBy(i => i.Cantidad)
                .ToListAsync();
        }

        // Métodos específicos para gestión de productos en inventario
        public async Task<bool> AddProductToInventoryAsync(int inventarioId, int productoId, long cantidad, long? stockMinimo = null, long? stockMaximo = null)
        {
            try
            {
                // Verificar si ya existe el producto en el inventario
                var existingProducto = await _farmaDbContext.InventarioProducto
                    .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                if (existingProducto != null)
                {
                    // Actualizar cantidad existente
                    existingProducto.Cantidad = (existingProducto.Cantidad ?? 0) + cantidad;
                    if (stockMinimo.HasValue) existingProducto.StockMinimo = stockMinimo;
                    if (stockMaximo.HasValue) existingProducto.StockMaximo = stockMaximo;

                    _farmaDbContext.InventarioProducto.Update(existingProducto);
                }
                else
                {
                    // Agregar nuevo producto al inventario
                    var nuevoInventarioProducto = new InventarioProducto
                    {
                        IdInventario = inventarioId,
                        IdProducto = productoId,
                        Cantidad = cantidad,
                        StockMinimo = stockMinimo,
                        StockMaximo = stockMaximo
                    };

                    _farmaDbContext.InventarioProducto.Add(nuevoInventarioProducto);
                }

                // Actualizar la cantidad total del inventario
                await UpdateInventoryTotalQuantityAsync(inventarioId);

                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en AddProductToInventoryAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveProductFromInventoryAsync(int inventarioId, int productoId)
        {
            try
            {
                var inventarioProducto = await _farmaDbContext.InventarioProducto
                    .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                if (inventarioProducto != null)
                {
                    _farmaDbContext.InventarioProducto.Remove(inventarioProducto);

                    // Actualizar la cantidad total del inventario
                    await UpdateInventoryTotalQuantityAsync(inventarioId);

                    await _farmaDbContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en RemoveProductFromInventoryAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProductQuantityAsync(int inventarioId, int productoId, long nuevaCantidad)
        {
            try
            {
                var inventarioProducto = await _farmaDbContext.InventarioProducto
                    .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                if (inventarioProducto != null)
                {
                    inventarioProducto.Cantidad = nuevaCantidad;
                    _farmaDbContext.InventarioProducto.Update(inventarioProducto);

                    // Actualizar la cantidad total del inventario
                    await UpdateInventoryTotalQuantityAsync(inventarioId);

                    await _farmaDbContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en UpdateProductQuantityAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<InventarioProducto>> GetProductsByInventoryAsync(int inventarioId)
        {
            return await _farmaDbContext.InventarioProducto
                .Include(ip => ip.IdProductoNavigation)
                    .ThenInclude(p => p.IdCategoriaNavigation)
                .Include(ip => ip.IdProductoNavigation)
                    .ThenInclude(p => p.IdProveedorNavigation)
                .Where(ip => ip.IdInventario == inventarioId)
                .OrderBy(ip => ip.IdProductoNavigation.NombreProducto)
                .ToListAsync();
        }

        public async Task<List<InventarioProducto>> GetLowStockProductsAsync(int? inventarioId = null)
        {
            var query = _farmaDbContext.InventarioProducto
                .Include(ip => ip.IdProductoNavigation)
                    .ThenInclude(p => p.IdCategoriaNavigation)
                .Include(ip => ip.IdInventarioNavigation)
                .Where(ip => ip.Cantidad <= ip.StockMinimo);

            if (inventarioId.HasValue)
            {
                query = query.Where(ip => ip.IdInventario == inventarioId.Value);
            }

            return await query
                .OrderBy(ip => ip.Cantidad)
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetInventoryStatsAsync(int inventarioId)
        {
            var inventario = await _farmaDbContext.Inventario
                .Include(i => i.InventarioProducto)
                    .ThenInclude(ip => ip.IdProductoNavigation)
                .FirstOrDefaultAsync(i => i.IdInventario == inventarioId);

            if (inventario == null)
                return new Dictionary<string, object>();

            var productos = inventario.InventarioProducto.ToList();

            var stats = new Dictionary<string, object>
            {
                ["TotalProductos"] = productos.Count,
                ["CantidadTotal"] = productos.Sum(p => p.Cantidad ?? 0),
                ["ProductosBajoStock"] = productos.Count(p => p.Cantidad <= p.StockMinimo),
                ["ProductosSinStock"] = productos.Count(p => (p.Cantidad ?? 0) == 0),
                ["ProductosSobreStock"] = productos.Count(p => p.StockMaximo.HasValue && p.Cantidad > p.StockMaximo),
                ["ValorTotalInventario"] = productos
                    .Where(p => p.IdProductoNavigation?.PrecioCompra.HasValue == true)
                    .Sum(p => (p.Cantidad ?? 0) * (decimal)(p.IdProductoNavigation?.PrecioCompra ?? 0))
            };

            return stats;
        }

        private async Task UpdateInventoryTotalQuantityAsync(int inventarioId)
        {
            var totalQuantity = await _farmaDbContext.InventarioProducto
                .Where(ip => ip.IdInventario == inventarioId)
                .SumAsync(ip => ip.Cantidad ?? 0);

            var inventario = await _farmaDbContext.Inventario.FindAsync(inventarioId);
            if (inventario != null)
            {
                inventario.Cantidad = (int)totalQuantity;
                inventario.UltimaActualizacion = DateTime.Now;
                _farmaDbContext.Inventario.Update(inventario);
            }
        }

        // Métodos de búsqueda y filtrado
        public async Task<List<Producto>> SearchAvailableProductsAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<Producto>();

            return await _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Where(p => p.Activo == true &&
                           (p.NombreProducto.Contains(searchTerm) ||
                            (p.DescrpcionProducto != null && p.DescrpcionProducto.Contains(searchTerm))))
                .OrderBy(p => p.NombreProducto)
                .Take(20) // Limitar resultados para performance
                .ToListAsync();
        }

        public async Task<bool> TransferProductBetweenInventoriesAsync(int fromInventoryId, int toInventoryId, int productoId, long cantidad)
        {
            try
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                // Verificar stock disponible en inventario origen
                var fromProduct = await _farmaDbContext.InventarioProducto
                    .FirstOrDefaultAsync(ip => ip.IdInventario == fromInventoryId && ip.IdProducto == productoId);

                if (fromProduct == null || (fromProduct.Cantidad ?? 0) < cantidad)
                {
                    return false; // No hay suficiente stock
                }

                // Reducir cantidad en inventario origen
                fromProduct.Cantidad = (fromProduct.Cantidad ?? 0) - cantidad;
                _farmaDbContext.InventarioProducto.Update(fromProduct);

                // Si la cantidad llega a 0, eliminar el registro
                if (fromProduct.Cantidad <= 0)
                {
                    _farmaDbContext.InventarioProducto.Remove(fromProduct);
                }

                // Agregar o incrementar cantidad en inventario destino
                var toProduct = await _farmaDbContext.InventarioProducto
                    .FirstOrDefaultAsync(ip => ip.IdInventario == toInventoryId && ip.IdProducto == productoId);

                if (toProduct != null)
                {
                    toProduct.Cantidad = (toProduct.Cantidad ?? 0) + cantidad;
                    _farmaDbContext.InventarioProducto.Update(toProduct);
                }
                else
                {
                    var newInventarioProducto = new InventarioProducto
                    {
                        IdInventario = toInventoryId,
                        IdProducto = productoId,
                        Cantidad = cantidad,
                        StockMinimo = fromProduct.StockMinimo,
                        StockMaximo = fromProduct.StockMaximo
                    };
                    _farmaDbContext.InventarioProducto.Add(newInventarioProducto);
                }

                // Actualizar totales de ambos inventarios
                await UpdateInventoryTotalQuantityAsync(fromInventoryId);
                await UpdateInventoryTotalQuantityAsync(toInventoryId);

                await _farmaDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en TransferProductBetweenInventoriesAsync: {ex.Message}");
                return false;
            }
        }
    }
}