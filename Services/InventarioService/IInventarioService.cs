using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.InventarioService
{
    public interface IInventarioService
    {
        // OPERACIONES BÁSICAS DE INVENTARIO
        Task<bool> AddUpdateAsync(Inventario inventario);
        Task<bool> DeleteAsync(int id_inventario);
        Task<List<Inventario>> GetAllAsync();
        Task<Inventario> GetByIdAsync(int id_inventario);
        Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Inventario>> GetLowStockAsync();

        // OPERACIONES DE PRODUCTOS EN INVENTARIO
        Task<bool> AddProductToInventoryAsync(int inventarioId, int productoId, long cantidad, long? stockMinimo = null, long? stockMaximo = null);
        Task<bool> RemoveProductFromInventoryAsync(int inventarioId, int productoId);
        Task<bool> UpdateProductQuantityAsync(int inventarioId, int productoId, long nuevaCantidad);
        Task<List<InventarioProducto>> GetProductsByInventoryAsync(int inventarioId);

        // CONSULTAS DE STOCK
        Task<List<InventarioProducto>> GetLowStockProductsAsync(int? inventarioId = null);
        Task<List<InventarioProducto>> GetCriticalStockProductsAsync(int? inventarioId = null);
        Task<bool> ProductExistsInInventoryAsync(int inventarioId, int productoId);
        Task<long> GetProductQuantityInInventoryAsync(int inventarioId, int productoId);

        // ESTADÍSTICAS Y REPORTES
        Task<Dictionary<string, object>> GetInventoryStatsAsync(int inventarioId);
        Task<List<dynamic>> GetInventorySummaryAsync();

        // BÚSQUEDA Y FILTRADO
        Task<List<Producto>> SearchAvailableProductsAsync(string searchTerm);

        // OPERACIONES AVANZADAS
        Task<bool> TransferProductBetweenInventoriesAsync(int fromInventoryId, int toInventoryId, int productoId, long cantidad);
        Task<bool> CreateInventoryWithProductsAsync(Inventario inventario, List<InventarioProducto> productos);
        Task<bool> ClearInventoryProductsAsync(int inventarioId);
        Task<bool> UpdateMultipleProductsAsync(int inventarioId, List<(int ProductoId, long Cantidad, long? StockMin, long? StockMax)> productos);
        Task<bool> CloneInventoryAsync(int sourceInventarioId, string newInventoryName);
    }
}