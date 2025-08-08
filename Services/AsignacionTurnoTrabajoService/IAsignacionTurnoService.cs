using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.AsignacionTurnoServices
{
    public interface IAsignacionTurnoService
    {
        Task<bool> AddUpdateAsync(AsignacionTurno asignacionTurno);
        Task<bool> DeleteAsync(int idAsignacion);
        Task<List<AsignacionTurno>> GetAllAsync();
        Task<AsignacionTurno> GetByIdAsync(int idAsignacion);
        Task<MPaginatedResult<AsignacionTurno>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<AsignacionTurno>> GetByPersonaAsync(int personaId);
        Task<List<AsignacionTurno>> GetBySucursalAsync(int sucursalId);
        Task<List<AsignacionTurno>> GetByTurnoAsync(int turnoId);
        Task<List<AsignacionTurno>> GetByDateRangeAsync(DateOnly fechaInicio, DateOnly fechaFin);
        Task<List<AsignacionTurno>> GetActiveAssignmentsAsync(DateOnly fecha);
    }
}