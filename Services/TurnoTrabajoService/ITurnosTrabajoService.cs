using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.TurnoTrabajoService
{
    public interface ITurnoTrabajoService
    {
        Task<bool> AddUpdateAsync(TurnoTrabajo turnoTrabajo);
        Task<bool> DeleteAsync(int id_turno);
        Task<List<TurnoTrabajo>> GetAllAsync();
        Task<TurnoTrabajo> GetByIdAsync(int id_turno);
        Task<MPaginatedResult<TurnoTrabajo>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        Task<List<TurnoTrabajo>> GetActiveTurnosAsync();
        Task<List<TurnoTrabajo>> GetTurnosByHorarioAsync(TimeSpan horaInicio, TimeSpan horaFin);
    }
}