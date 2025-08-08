using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.GeneroServices
{
    public interface IGeneroServices
    {

        Task<bool> AddUpdateAsync(Genero genero);
        Task<bool> DeleteAsync(int genero);
        Task<List<Genero>> GetAllAsync();
        Task<Genero> GetByIdAsync(int id_genero);

        Task<MPaginatedResult<Genero>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
    }
}
