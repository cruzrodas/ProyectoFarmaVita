using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.ProductoService
{
    public interface IProductoService
    {
        Task<bool> AddUpdateAsync(Producto producto);
        Task<bool> DeleteAsync(int idProducto);
        Task<List<Producto>> GetAllAsync();
        Task<Producto> GetByIdAsync(int idProducto);
        Task<MPaginatedResult<Producto>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Producto>> GetActivosAsync();
        Task<List<Producto>> GetByCategoriaAsync(int categoriaId);
        Task<List<Producto>> GetByProveedorAsync(int proveedorId);
        Task<bool> ExistsAsync(string nombreProducto, int? excludeId = null);
    }
}
