using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.ProveedorServices
{
    public interface IProveedorService
    {
        Task<bool> AddUpdateAsync(Proveedor proveedor);
        Task<bool> DeleteAsync(int idProveedor);
        Task<List<Proveedor>> GetAllAsync();
        Task<Proveedor> GetByIdAsync(int idProveedor);
        Task<MPaginatedResult<Proveedor>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Proveedor>> GetActiveAsync();
        Task<List<Proveedor>> SearchByNameAsync(string nombreProveedor);
        Task<bool> ExistsAsync(string nombreProveedor, int? excludeId = null);
        Task<int> GetProductCountByProveedorAsync(int idProveedor);
        Task<List<Proveedor>> GetByPersonaContactoAsync(int personaContactoId);
    }
}