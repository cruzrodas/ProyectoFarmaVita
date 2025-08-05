using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.PersonaServices
{
    public interface IPersonaService
    {
        Task<bool> AddAsync(Persona persona);
        Task<bool> UpdateAsync(Persona persona);
        Task<bool> DeleteAsync(int idPersona);
        Task<List<Persona>> GetAllAsync();
        Task<Persona?> GetByIdAsync(int idPersona);
        Task<List<Persona>> GetActiveAsync();
        Task<List<Persona>> GetByRolAsync(int idRol);
        Task<List<Persona>> GetBySucursalAsync(int idSucursal);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByDpiAsync(int dpi);
        Task<Persona?> GetByEmailAsync(string email);
        Task<List<Persona>> SearchAsync(string searchTerm);

        // Método actualizado con parámetros adicionales para filtros
        Task<(List<Persona> personas, int totalCount)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            string searchTerm = "",
            bool sortAscending = true,
            string sortBy = "Nombre",
            bool mostrarInactivos = false,
            int? rolId = null,
            int? sucursalId = null,
            int? generoId = null,
            int? estadoCivilId = null);

        // Métodos para obtener datos de filtros
        Task<List<Rol>> GetRolesAsync();
        Task<List<Sucursal>> GetSucursalesAsync();
        Task<List<Genero>> GetGenerosAsync();
        Task<List<EstadoCivil>> GetEstadosCivilesAsync();

        // Agrega estos métodos a tu interfaz existente
        Task<bool> ExistsByEmailExcludingIdAsync(string email, int excludeId);
        Task<bool> ExistsByDpiExcludingIdAsync(int dpi, int excludeId);
    }
}
