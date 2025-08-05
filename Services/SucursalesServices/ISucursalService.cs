using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.SucursalServices
{
    public interface ISucursalService
    {
        Task<(List<Sucursal> sucursales, int totalCount)> GetPaginatedAsync(
     int pageNumber,
     int pageSize,
     string searchTerm = "",
     bool sortAscending = true,
     string sortBy = "NombreSucursal",
     bool mostrarInactivos = false,
     bool mostrarTodos = false,
     int? responsableId = null,
     int? inventarioId = null,
     int? departamentoId = null,
     int? municipioId = null);

        Task<bool> AddAsync(Sucursal sucursal);
        Task<bool> UpdateAsync(Sucursal sucursal);
        Task<bool> DeleteAsync(int idSucursal);
        Task<List<Sucursal>> GetAllAsync();
        Task<Sucursal?> GetByIdAsync(int idSucursal);
        Task<List<Sucursal>> GetActiveAsync();

        // Métodos de búsqueda y filtrado
        Task<List<Sucursal>> GetByResponsableAsync(int idResponsable);
        Task<List<Sucursal>> SearchAsync(string searchTerm);

        // Métodos de validación
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByNombreAsync(string nombre);
        Task<bool> ExistsByEmailExcludingIdAsync(string email, int excludeId);
        Task<bool> ExistsByNombreExcludingIdAsync(string nombre, int excludeId);

        // Métodos para obtener datos de referencia
        Task<List<Persona>> GetPersonasAsync(); // Para responsables
        Task<List<Inventario>> GetInventariosAsync();
        Task<List<Telefono>> GetTelefonosAsync();
        Task<List<Direccion>> GetDireccionesAsync();
        Task<List<Departamento>> GetDepartamentosAsync();
        Task<List<Municipio>> GetMunicipiosAsync();
        Task<List<Municipio>> GetMunicipiosByDepartamentoAsync(int idDepartamento);

    }
}