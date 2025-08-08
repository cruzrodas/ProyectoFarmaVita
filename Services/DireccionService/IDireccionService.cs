using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.DireccionServices
{
    public interface IDireccionService
    {
        Task<bool> AddUpdateAsync(Direccion direccion);
        Task<int> AddAsync(Direccion direccion); // Método especial que retorna el ID
        Task<bool> DeleteAsync(int id_direccion);
        Task<List<Direccion>> GetAllAsync();
        Task<Direccion> GetByIdAsync(int id_direccion);
        Task<MPaginatedResult<Direccion>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Direccion>> GetByMunicipioIdAsync(int municipioId);
    }
}