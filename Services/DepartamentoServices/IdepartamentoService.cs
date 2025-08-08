using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.DepartamentoServices
{
    public interface IdepartamentoService
    {
        Task<bool> AddUpdateAsync(Departamento departamento);
        Task<bool> DeleteAsync(int id_departamento);
        Task<List<Departamento>> GetAllAsync();
        Task<Departamento> GetByIdAsync(int id_departamento);

        Task<MPaginatedResult<Departamento>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
    }
}
