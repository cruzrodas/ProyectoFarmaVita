using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.SucursalServices
{
    public interface ISucursalService
    {
        Task<bool> AddUpdateAsync(Sucursal sucursal);
        Task<bool> DeleteAsync(int id_sucursal);
        Task<List<Sucursal>> GetAllAsync();
        Task<Sucursal> GetByIdAsync(int id_sucursal);
        Task<MPaginatedResult<Sucursal>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Sucursal>> GetByResponsableAsync(int responsableId);

    }
}