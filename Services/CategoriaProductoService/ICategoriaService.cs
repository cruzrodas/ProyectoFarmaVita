using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.CategoriaProductoService
{
    public interface ICategoriaService
    {
        Task<bool> AddUpdateAsync(Categoria categoria);
        Task<bool> DeleteAsync(int idCategoria);
        Task<List<Categoria>> GetAllAsync();
        Task<Categoria> GetByIdAsync(int idCategoria);
        Task<MPaginatedResult<Categoria>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Categoria>> SearchByNameAsync(string nombreCategoria);
        Task<bool> ExistsAsync(string nombreCategoria, int? excludeId = null);
        Task<int> GetProductCountByCategoriaAsync(int idCategoria);
    }
}
