using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.MunicipioService
{
    public interface IMunicipioService
    {
        Task<bool> AddUpdateAsync(Municipio municipio);
        Task<bool> DeleteAsync(int id_municipio);
        Task<List<Municipio>> GetAllAsync();
        Task<Municipio> GetByIdAsync(int id_municipio);
        Task<MPaginatedResult<Municipio>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<Municipio>> GetByDepartamentoIdAsync(int departamentoId);
    }
}
