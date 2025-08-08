using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.EstadoCivilServices
{
    public interface IEstadoCivil
    {
Task<bool> AddUpdateAsync(EstadoCivil estadocivil);
        Task<bool> DeleteAsync(int estadocivil);
        Task<List<EstadoCivil>> GetAllAsync();
        Task<EstadoCivil> GetByIdAsync(int id_estadocivil);

        Task<MPaginatedResult<EstadoCivil>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        
    }
}

