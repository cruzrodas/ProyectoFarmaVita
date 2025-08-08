using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.InventarioServices
{
    public interface IInventarioService
    {
        Task<bool> AddUpdateAsync(Inventario inventario);
        Task<bool> DeleteAsync(int id_inventario);
        Task<List<Inventario>> GetAllAsync();
        Task<Inventario> GetByIdAsync(int id_inventario);
        Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Inventario>> GetByProductoIdAsync(int productoId);
        Task<List<Inventario>> GetLowStockAsync(); // Inventarios con stock bajo
    }
}