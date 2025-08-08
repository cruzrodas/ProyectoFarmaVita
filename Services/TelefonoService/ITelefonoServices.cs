using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.TelefonoServices
{
    public interface ITelefonoService
    {
        Task<bool> AddUpdateAsync(Telefono telefono);
        Task<int> AddAsync(Telefono telefono); // Método especial que retorna el ID
        Task<bool> DeleteAsync(int id_telefono);
        Task<List<Telefono>> GetAllAsync();
        Task<Telefono> GetByIdAsync(int id_telefono);
        Task<MPaginatedResult<Telefono>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
    }
}