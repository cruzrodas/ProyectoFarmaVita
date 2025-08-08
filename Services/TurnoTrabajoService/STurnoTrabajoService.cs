using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.TurnoTrabajoService;

namespace ProyectoFarmaVita.Services.TurnoTrabajoServices
{
    public class STurnoTrabajoService : ITurnoTrabajoService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public STurnoTrabajoService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(TurnoTrabajo turnoTrabajo)
        {
            if (turnoTrabajo.IdTurno > 0)
            {
                // Buscar el turno existente en la base de datos
                var existingTurno = await _farmaDbContext.TurnoTrabajo.FindAsync(turnoTrabajo.IdTurno);

                if (existingTurno != null)
                {
                    // Actualizar las propiedades existentes
                    existingTurno.NombreTurno = turnoTrabajo.NombreTurno;
                    existingTurno.HoraInicio = turnoTrabajo.HoraInicio;
                    existingTurno.HoraFin = turnoTrabajo.HoraFin;
                    existingTurno.Descripcion = turnoTrabajo.Descripcion;

                    // Marcar el turno como modificado
                    _farmaDbContext.TurnoTrabajo.Update(existingTurno);
                }
                else
                {
                    return false; // Si no se encontró el turno, devolver false
                }
            }
            else
            {
                turnoTrabajo.Activo = true;

                // Si no hay ID, se trata de un nuevo turno, agregarlo
                _farmaDbContext.TurnoTrabajo.Add(turnoTrabajo);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_turno)
        {
            var turno = await _farmaDbContext.TurnoTrabajo.FindAsync(id_turno);
            if (turno != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                turno.Activo = false;

                _farmaDbContext.TurnoTrabajo.Update(turno);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<TurnoTrabajo>> GetAllAsync()
        {
            return await _farmaDbContext.TurnoTrabajo
                .Where(t => t.Activo == true)
                .OrderBy(t => t.HoraInicio)
                .ToListAsync();
        }

        public async Task<TurnoTrabajo> GetByIdAsync(int id_turno)
        {
            try
            {
                var result = await _farmaDbContext.TurnoTrabajo
                    .Where(t => t.Activo == true)
                    .FirstOrDefaultAsync(t => t.IdTurno == id_turno);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el turno de trabajo con ID {id_turno}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el turno de trabajo", ex);
            }
        }

        public async Task<MPaginatedResult<TurnoTrabajo>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.TurnoTrabajo
                .Where(t => t.Activo == true); // Excluir los eliminados

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.NombreTurno.Contains(searchTerm) ||
                                       (t.Descripcion != null && t.Descripcion.Contains(searchTerm)));
            }

            // Ordenamiento basado en el campo NombreTurno
            query = sortAscending
                ? query.OrderBy(t => t.HoraInicio).ThenBy(t => t.NombreTurno)
                : query.OrderByDescending(t => t.HoraInicio).ThenByDescending(t => t.NombreTurno);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<TurnoTrabajo>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<TurnoTrabajo>> GetActiveTurnosAsync()
        {
            return await _farmaDbContext.TurnoTrabajo
                .Where(t => t.Activo == true)
                .OrderBy(t => t.HoraInicio)
                .ToListAsync();
        }

        public async Task<List<TurnoTrabajo>> GetTurnosByHorarioAsync(TimeSpan horaInicio, TimeSpan horaFin)
        {
            return await _farmaDbContext.TurnoTrabajo
                .Where(t => t.Activo == true &&
                          t.HoraInicio.HasValue &&
                          t.HoraFin.HasValue &&
                          t.HoraInicio.Value.TimeOfDay >= horaInicio &&
                          t.HoraFin.Value.TimeOfDay <= horaFin)
                .OrderBy(t => t.HoraInicio)
                .ToListAsync();
        }
    }
}