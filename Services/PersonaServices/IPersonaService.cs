using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.PersonaServices
{
    public interface IPersonaService
    {
        #region Métodos CRUD Principales

        /// <summary>
        /// Obtiene personas con paginación y filtros
        /// </summary>
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

        /// <summary>
        /// Agrega una nueva persona
        /// </summary>
        Task<bool> AddAsync(Persona persona);

        /// <summary>
        /// Actualiza una persona existente
        /// </summary>
        Task<bool> UpdateAsync(Persona persona);

        /// <summary>
        /// Elimina una persona (soft delete)
        /// </summary>
        Task<bool> DeleteAsync(int idPersona);

        #endregion

        #region Métodos de Consulta

        /// <summary>
        /// Obtiene todas las personas
        /// </summary>
        Task<List<Persona>> GetAllAsync();

        /// <summary>
        /// Obtiene una persona por ID
        /// </summary>
        Task<Persona?> GetByIdAsync(int idPersona);

        /// <summary>
        /// Obtiene solo las personas activas
        /// </summary>
        Task<List<Persona>> GetActiveAsync();

        /// <summary>
        /// Obtiene personas por rol
        /// </summary>
        Task<List<Persona>> GetByRolAsync(int idRol);

        /// <summary>
        /// Obtiene personas por sucursal
        /// </summary>
        Task<List<Persona>> GetBySucursalAsync(int idSucursal);

        /// <summary>
        /// Busca personas por término
        /// </summary>
        Task<List<Persona>> SearchAsync(string searchTerm);

        /// <summary>
        /// Obtiene una persona por email
        /// </summary>
        Task<Persona?> GetByEmailAsync(string email);

        #endregion

        #region Métodos de Validación

        /// <summary>
        /// Verifica si existe un email
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>
        /// Verifica si existe un DPI
        /// </summary>
        Task<bool> ExistsByDpiAsync(long dpi);

        /// <summary>
        /// Verifica si existe un email excluyendo un ID específico
        /// </summary>
        Task<bool> ExistsByEmailExcludingIdAsync(string email, int excludeId);

        /// <summary>
        /// Verifica si existe un DPI excluyendo un ID específico
        /// </summary>
        Task<bool> ExistsByDpiExcludingIdAsync(long dpi, int excludeId);

        #endregion

        #region Métodos de Seguridad

        /// <summary>
        /// Cambia la contraseña de una persona
        /// </summary>
        Task<bool> ChangePasswordAsync(int idPersona, string newPassword);

        #endregion

        #region Datos de Referencia

        /// <summary>
        /// Obtiene todos los roles activos
        /// </summary>
        Task<List<Rol>> GetRolesAsync();

        /// <summary>
        /// Obtiene todas las sucursales activas
        /// </summary>
        Task<List<Sucursal>> GetSucursalesAsync();

        /// <summary>
        /// Obtiene todos los géneros activos
        /// </summary>
        Task<List<Genero>> GetGenerosAsync();

        /// <summary>
        /// Obtiene todos los estados civiles activos
        /// </summary>
        Task<List<EstadoCivil>> GetEstadosCivilesAsync();

        /// <summary>
        /// Obtiene todos los departamentos
        /// </summary>
        Task<List<Departamento>> GetDepartamentosAsync();

        /// <summary>
        /// Obtiene todos los municipios
        /// </summary>
        Task<List<Municipio>> GetMunicipiosAsync();

        #endregion

        #region Métodos para Teléfonos

        /// <summary>
        /// Crea un nuevo teléfono y devuelve su ID
        /// </summary>
        Task<int?> CreateTelefonoAsync(int numeroTelefonico);

        /// <summary>
        /// Actualiza un teléfono existente
        /// </summary>
        Task<int?> UpdateTelefonoAsync(int idTelefono, int numeroTelefonico);

        /// <summary>
        /// Obtiene un teléfono por ID
        /// </summary>
        Task<Telefono?> GetTelefonoByIdAsync(int idTelefono);

        #endregion

        #region Métodos para Direcciones

        /// <summary>
        /// Crea una nueva dirección y devuelve su ID
        /// </summary>
        Task<int?> CreateDireccionAsync(string direccion, int idMunicipio);

        /// <summary>
        /// Actualiza una dirección existente
        /// </summary>
        Task<int?> UpdateDireccionAsync(int idDireccion, string direccion, int idMunicipio);

        /// <summary>
        /// Obtiene una dirección por ID
        /// </summary>
        Task<Direccion?> GetDireccionByIdAsync(int idDireccion);

        #endregion

        #region Métodos de Utilidad y Estadísticas

        /// <summary>
        /// Obtiene el total de personas
        /// </summary>
        Task<int> GetTotalPersonasAsync();

        /// <summary>
        /// Obtiene el total de personas activas
        /// </summary>
        Task<int> GetTotalPersonasActivasAsync();

        /// <summary>
        /// Obtiene estadísticas de personas por rol
        /// </summary>
        Task<Dictionary<string, int>> GetPersonasPorRolAsync();

        /// <summary>
        /// Obtiene las personas más recientes
        /// </summary>
        Task<List<Persona>> GetPersonasRecientesAsync(int cantidad = 10);

        #endregion
    }
}