using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.InventarioService;

namespace ProyectoFarmaVita.Services.InventarioService
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
            // Usar la estrategia de ejecución configurada
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    if (inventario.IdInventario > 0)
                    {
                        // Actualizar inventario existente
                        var existingInventario = await _farmaDbContext.Inventario.FindAsync(inventario.IdInventario);

                        if (existingInventario != null)
                        {
                            existingInventario.NombreInventario = inventario.NombreInventario;
                            existingInventario.UltimaActualizacion = DateTime.Now;

                            _farmaDbContext.Inventario.Update(existingInventario);
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                    else
                    {
                        // Crear nuevo inventario
                        inventario.UltimaActualizacion = DateTime.Now;
                        _farmaDbContext.Inventario.Add(inventario);
                    }

                    await _farmaDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en AddUpdateAsync: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> DeleteAsync(int id_inventario)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    var inventario = await _farmaDbContext.Inventario
                        .Include(i => i.Sucursal)
                        .Include(i => i.InventarioProducto)
                        .FirstOrDefaultAsync(i => i.IdInventario == id_inventario);

                    if (inventario != null)
                    {
                        // Verificar si el inventario tiene dependencias críticas
                        bool hasCriticalDependencies = inventario.Sucursal?.Any() == true;

                        if (hasCriticalDependencies)
                        {
                            await transaction.RollbackAsync();
                            return false; // No se puede eliminar si tiene sucursales asociadas
                        }

                        // Primero eliminar todos los productos del inventario
                        if (inventario.InventarioProducto?.Any() == true)
                        {
                            _farmaDbContext.InventarioProducto.RemoveRange(inventario.InventarioProducto);
                        }

                        // Luego eliminar el inventario
                        _farmaDbContext.Inventario.Remove(inventario);
                        await _farmaDbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return true;
                    }

                    await transaction.RollbackAsync();
                    return false;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en DeleteAsync: {ex.Message}");
                    return false;
                }
            });
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

            // Ordenamiento
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
                .Where(i => i.InventarioProducto.Any(ip =>
                    ip.StockMinimo.HasValue &&
                    ip.Cantidad.HasValue &&
                    ip.Cantidad <= ip.StockMinimo))
                .OrderBy(i => i.NombreInventario)
                .ToListAsync();
        }

        // MÉTODOS PARA GESTIÓN DE PRODUCTOS EN INVENTARIO

        public async Task<bool> AddProductToInventoryAsync(int inventarioId, int productoId, long cantidad, long? stockMinimo = null, long? stockMaximo = null)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    // Verificar si ya existe el producto en el inventario
                    var existingProducto = await _farmaDbContext.InventarioProducto
                        .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                    if (existingProducto != null)
                    {
                        // Actualizar producto existente
                        existingProducto.Cantidad = cantidad; // Reemplazar cantidad en lugar de sumar
                        if (stockMinimo.HasValue) existingProducto.StockMinimo = stockMinimo;
                        if (stockMaximo.HasValue) existingProducto.StockMaximo = stockMaximo;

                        _farmaDbContext.InventarioProducto.Update(existingProducto);
                    }
                    else
                    {
                        // Generar nuevo ID para InventarioProducto
                        var maxId = await _farmaDbContext.InventarioProducto
                            .MaxAsync(ip => (int?)ip.IdInventarioProducto) ?? 0;

                        // Agregar nuevo producto al inventario
                        var nuevoInventarioProducto = new InventarioProducto
                        {
                            IdInventarioProducto = maxId + 1,
                            IdInventario = inventarioId,
                            IdProducto = productoId,
                            Cantidad = cantidad,
                            StockMinimo = stockMinimo,
                            StockMaximo = stockMaximo
                        };

                        _farmaDbContext.InventarioProducto.Add(nuevoInventarioProducto);
                    }

                    // Actualizar fecha de última modificación del inventario
                    await UpdateInventoryTimestampAsync(inventarioId);

                    await _farmaDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en AddProductToInventoryAsync: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> RemoveProductFromInventoryAsync(int inventarioId, int productoId)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    var inventarioProducto = await _farmaDbContext.InventarioProducto
                        .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                    if (inventarioProducto != null)
                    {
                        _farmaDbContext.InventarioProducto.Remove(inventarioProducto);
                        await UpdateInventoryTimestampAsync(inventarioId);

                        await _farmaDbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return true;
                    }

                    await transaction.RollbackAsync();
                    return false;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en RemoveProductFromInventoryAsync: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> UpdateProductQuantityAsync(int inventarioId, int productoId, long nuevaCantidad)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    var inventarioProducto = await _farmaDbContext.InventarioProducto
                        .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                    if (inventarioProducto != null)
                    {
                        inventarioProducto.Cantidad = nuevaCantidad;
                        _farmaDbContext.InventarioProducto.Update(inventarioProducto);

                        await UpdateInventoryTimestampAsync(inventarioId);

                        await _farmaDbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return true;
                    }

                    await transaction.RollbackAsync();
                    return false;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en UpdateProductQuantityAsync: {ex.Message}");
                    return false;
                }
            });
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
                .Where(ip => ip.StockMinimo.HasValue &&
                           ip.Cantidad.HasValue &&
                           ip.Cantidad <= ip.StockMinimo);

            if (inventarioId.HasValue)
            {
                query = query.Where(ip => ip.IdInventario == inventarioId.Value);
            }

            return await query
                .OrderBy(ip => ip.Cantidad)
                .ThenBy(ip => ip.IdProductoNavigation.NombreProducto)
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

            var productos = inventario.InventarioProducto.Where(ip => ip.IdProductoNavigation != null).ToList();

            var stats = new Dictionary<string, object>
            {
                ["TotalProductos"] = productos.Count,
                ["CantidadTotal"] = productos.Sum(p => p.Cantidad ?? 0),
                ["ProductosBajoStock"] = productos.Count(p =>
                    p.StockMinimo.HasValue &&
                    p.Cantidad.HasValue &&
                    p.Cantidad <= p.StockMinimo),
                ["ProductosSinStock"] = productos.Count(p => (p.Cantidad ?? 0) == 0),
                ["ProductosSobreStock"] = productos.Count(p =>
                    p.StockMaximo.HasValue &&
                    p.Cantidad.HasValue &&
                    p.Cantidad > p.StockMaximo),
                ["ValorTotalInventario"] = productos
                    .Where(p => p.IdProductoNavigation?.PrecioCompra.HasValue == true && p.Cantidad.HasValue)
                    .Sum(p => (p.Cantidad ?? 0) * (decimal)(p.IdProductoNavigation?.PrecioCompra ?? 0))
            };

            return stats;
        }

        private async Task UpdateInventoryTimestampAsync(int inventarioId)
        {
            var inventario = await _farmaDbContext.Inventario.FindAsync(inventarioId);
            if (inventario != null)
            {
                inventario.UltimaActualizacion = DateTime.Now;
                _farmaDbContext.Inventario.Update(inventario);
            }
        }

        // MÉTODOS DE BÚSQUEDA Y FILTRADO

        public async Task<List<Producto>> SearchAvailableProductsAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 2)
                return new List<Producto>();

            return await _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Where(p => p.Activo == true &&
                           (p.NombreProducto.Contains(searchTerm) ||
                            (p.DescrpcionProducto != null && p.DescrpcionProducto.Contains(searchTerm))))
                .OrderBy(p => p.NombreProducto)
                .Take(20)
                .ToListAsync();
        }

        // MÉTODO OPTIMIZADO PARA TRANSFERENCIAS
        public async Task<bool> TransferProductBetweenInventoriesAsync(int fromInventoryId, int toInventoryId, int productoId, long cantidad)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    // Verificar stock disponible en inventario origen
                    var fromProduct = await _farmaDbContext.InventarioProducto
                        .FirstOrDefaultAsync(ip => ip.IdInventario == fromInventoryId && ip.IdProducto == productoId);

                    if (fromProduct == null || (fromProduct.Cantidad ?? 0) < cantidad)
                    {
                        await transaction.RollbackAsync();
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
                        var maxId = await _farmaDbContext.InventarioProducto
                            .MaxAsync(ip => (int?)ip.IdInventarioProducto) ?? 0;

                        var newInventarioProducto = new InventarioProducto
                        {
                            IdInventarioProducto = maxId + 1,
                            IdInventario = toInventoryId,
                            IdProducto = productoId,
                            Cantidad = cantidad,
                            StockMinimo = fromProduct.StockMinimo,
                            StockMaximo = fromProduct.StockMaximo
                        };
                        _farmaDbContext.InventarioProducto.Add(newInventarioProducto);
                    }

                    // Actualizar timestamps de ambos inventarios
                    await UpdateInventoryTimestampAsync(fromInventoryId);
                    await UpdateInventoryTimestampAsync(toInventoryId);

                    await _farmaDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en TransferProductBetweenInventoriesAsync: {ex.Message}");
                    return false;
                }
            });
        }

        // MÉTODO OPTIMIZADO PARA CREAR INVENTARIO CON PRODUCTOS
        public async Task<bool> CreateInventoryWithProductsAsync(Inventario inventario, List<InventarioProducto> productos)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    // Crear el inventario
                    inventario.UltimaActualizacion = DateTime.Now;
                    _farmaDbContext.Inventario.Add(inventario);
                    await _farmaDbContext.SaveChangesAsync(); // Guardar para obtener el ID

                    // Agregar los productos si existen
                    if (productos?.Any() == true)
                    {
                        var maxId = await _farmaDbContext.InventarioProducto
                            .MaxAsync(ip => (int?)ip.IdInventarioProducto) ?? 0;

                        foreach (var producto in productos)
                        {
                            producto.IdInventarioProducto = ++maxId;
                            producto.IdInventario = inventario.IdInventario;
                            _farmaDbContext.InventarioProducto.Add(producto);
                        }

                        await _farmaDbContext.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en CreateInventoryWithProductsAsync: {ex.Message}");
                    return false;
                }
            });
        }

        // MÉTODO PARA LIMPIAR PRODUCTOS DE UN INVENTARIO
        public async Task<bool> ClearInventoryProductsAsync(int inventarioId)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    var productosInventario = await _farmaDbContext.InventarioProducto
                        .Where(ip => ip.IdInventario == inventarioId)
                        .ToListAsync();

                    if (productosInventario.Any())
                    {
                        _farmaDbContext.InventarioProducto.RemoveRange(productosInventario);
                        await UpdateInventoryTimestampAsync(inventarioId);
                        await _farmaDbContext.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en ClearInventoryProductsAsync: {ex.Message}");
                    return false;
                }
            });
        }

        // MÉTODO PARA OBTENER RESUMEN DE INVENTARIOS
        public async Task<List<dynamic>> GetInventorySummaryAsync()
        {
            return await _farmaDbContext.Inventario
                .Include(i => i.InventarioProducto)
                    .ThenInclude(ip => ip.IdProductoNavigation)
                .Select(i => new
                {
                    i.IdInventario,
                    i.NombreInventario,
                    i.UltimaActualizacion,
                    TotalProductos = i.InventarioProducto.Count,
                    TotalCantidad = i.InventarioProducto.Sum(ip => ip.Cantidad ?? 0),
                    ProductosBajoStock = i.InventarioProducto.Count(ip =>
                        ip.StockMinimo.HasValue &&
                        ip.Cantidad.HasValue &&
                        ip.Cantidad <= ip.StockMinimo),
                    ValorTotal = i.InventarioProducto
                        .Where(ip => ip.IdProductoNavigation.PrecioCompra.HasValue && ip.Cantidad.HasValue)
                        .Sum(ip => (ip.Cantidad ?? 0) * (decimal)(ip.IdProductoNavigation.PrecioCompra ?? 0))
                })
                .Cast<dynamic>()
                .ToListAsync();
        }

        // MÉTODO PARA VALIDAR EXISTENCIA DE PRODUCTO EN INVENTARIO
        public async Task<bool> ProductExistsInInventoryAsync(int inventarioId, int productoId)
        {
            return await _farmaDbContext.InventarioProducto
                .AnyAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);
        }

        // MÉTODO PARA OBTENER CANTIDAD DE UN PRODUCTO EN INVENTARIO
        public async Task<long> GetProductQuantityInInventoryAsync(int inventarioId, int productoId)
        {
            var inventarioProducto = await _farmaDbContext.InventarioProducto
                .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

            return inventarioProducto?.Cantidad ?? 0;
        }

        // MÉTODO PARA ACTUALIZAR MÚLTIPLES PRODUCTOS A LA VEZ
        public async Task<bool> UpdateMultipleProductsAsync(int inventarioId, List<(int ProductoId, long Cantidad, long? StockMin, long? StockMax)> productos)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    foreach (var (productoId, cantidad, stockMin, stockMax) in productos)
                    {
                        // Reutilizar la lógica existente pero sin transacción anidada
                        var existingProducto = await _farmaDbContext.InventarioProducto
                            .FirstOrDefaultAsync(ip => ip.IdInventario == inventarioId && ip.IdProducto == productoId);

                        if (existingProducto != null)
                        {
                            existingProducto.Cantidad = cantidad;
                            if (stockMin.HasValue) existingProducto.StockMinimo = stockMin;
                            if (stockMax.HasValue) existingProducto.StockMaximo = stockMax;
                            _farmaDbContext.InventarioProducto.Update(existingProducto);
                        }
                        else
                        {
                            var maxId = await _farmaDbContext.InventarioProducto
                                .MaxAsync(ip => (int?)ip.IdInventarioProducto) ?? 0;

                            var nuevoInventarioProducto = new InventarioProducto
                            {
                                IdInventarioProducto = maxId + 1,
                                IdInventario = inventarioId,
                                IdProducto = productoId,
                                Cantidad = cantidad,
                                StockMinimo = stockMin,
                                StockMaximo = stockMax
                            };
                            _farmaDbContext.InventarioProducto.Add(nuevoInventarioProducto);
                        }
                    }

                    await UpdateInventoryTimestampAsync(inventarioId);
                    await _farmaDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en UpdateMultipleProductsAsync: {ex.Message}");
                    return false;
                }
            });
        }

        // MÉTODO PARA OBTENER PRODUCTOS CON STOCK CRÍTICO
        public async Task<List<InventarioProducto>> GetCriticalStockProductsAsync(int? inventarioId = null)
        {
            var query = _farmaDbContext.InventarioProducto
                .Include(ip => ip.IdProductoNavigation)
                    .ThenInclude(p => p.IdCategoriaNavigation)
                .Include(ip => ip.IdInventarioNavigation)
                .Where(ip => ip.StockMinimo.HasValue &&
                           ip.Cantidad.HasValue &&
                           ip.Cantidad <= (ip.StockMinimo * 0.5)); // Stock crítico: 50% del mínimo

            if (inventarioId.HasValue)
            {
                query = query.Where(ip => ip.IdInventario == inventarioId.Value);
            }

            return await query
                .OrderBy(ip => ip.Cantidad)
                .ToListAsync();
        }

        // MÉTODO PARA CLONAR INVENTARIO
        public async Task<bool> CloneInventoryAsync(int sourceInventarioId, string newInventoryName)
        {
            var strategy = _farmaDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _farmaDbContext.Database.BeginTransactionAsync();

                try
                {
                    // Obtener inventario origen
                    var sourceInventario = await GetByIdAsync(sourceInventarioId);

                    // Crear nuevo inventario
                    var newInventario = new Inventario
                    {
                        NombreInventario = newInventoryName,
                        UltimaActualizacion = DateTime.Now
                    };

                    _farmaDbContext.Inventario.Add(newInventario);
                    await _farmaDbContext.SaveChangesAsync();

                    // Clonar productos del inventario origen
                    if (sourceInventario.InventarioProducto?.Any() == true)
                    {
                        var maxId = await _farmaDbContext.InventarioProducto
                            .MaxAsync(ip => (int?)ip.IdInventarioProducto) ?? 0;

                        foreach (var producto in sourceInventario.InventarioProducto)
                        {
                            var newProducto = new InventarioProducto
                            {
                                IdInventarioProducto = ++maxId,
                                IdInventario = newInventario.IdInventario,
                                IdProducto = producto.IdProducto,
                                Cantidad = 0, // Empezar con cantidad 0
                                StockMinimo = producto.StockMinimo,
                                StockMaximo = producto.StockMaximo
                            };

                            _farmaDbContext.InventarioProducto.Add(newProducto);
                        }

                        await _farmaDbContext.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error en CloneInventoryAsync: {ex.Message}");
                    return false;
                }
            });
        }
    }
}