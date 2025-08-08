using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.InventarioServices
{
    public interface IInventarioService
    {
        // Métodos básicos CRUD
        Task<bool> AddUpdateAsync(Inventario inventario);
        Task<bool> DeleteAsync(int id_inventario);
        Task<List<Inventario>> GetAllAsync();
        Task<Inventario> GetByIdAsync(int id_inventario);
        Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);

        // Métodos para inventarios con stock bajo
        Task<List<Inventario>> GetLowStockAsync();

        // Métodos para gestión de productos en inventario
        Task<bool> AddProductToInventoryAsync(int inventarioId, int productoId, long cantidad, long? stockMinimo = null, long? stockMaximo = null);
        Task<bool> RemoveProductFromInventoryAsync(int inventarioId, int productoId);
        Task<bool> UpdateProductQuantityAsync(int inventarioId, int productoId, long nuevaCantidad);

        // Métodos de consulta de productos en inventario
        Task<List<InventarioProducto>> GetProductsByInventoryAsync(int inventarioId);
        Task<List<InventarioProducto>> GetLowStockProductsAsync(int? inventarioId = null);

        // Métodos de estadísticas
        Task<Dictionary<string, object>> GetInventoryStatsAsync(int inventarioId);

        // Métodos de búsqueda
        Task<List<Producto>> SearchAvailableProductsAsync(string searchTerm);

        // Métodos de transferencia
        Task<bool> TransferProductBetweenInventoriesAsync(int fromInventoryId, int toInventoryId, int productoId, long cantidad);
    }
}