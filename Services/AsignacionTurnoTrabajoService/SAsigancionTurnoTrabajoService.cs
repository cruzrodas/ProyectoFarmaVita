using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.AsignacionTurnoServices
{
    public class SAsignacionTurnoService : IAsignacionTurnoService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SAsignacionTurnoService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(AsignacionTurno asignacionTurno)
        {
            if (asignacionTurno.IdAsignacion > 0)
            {
                // Buscar la asignación existente en la base de datos
                var existingAsignacion = await _farmaDbContext.AsignacionTurno.FindAsync(asignacionTurno.IdAsignacion);

                if (existingAsignacion != null)
                {
                    // Actualizar las propiedades existentes
                    existingAsignacion.IdPersona = asignacionTurno.IdPersona;
                    existingAsignacion.IdTurno = asignacionTurno.IdTurno;
                    existingAsignacion.IdSucursal = asignacionTurno.IdSucursal;
                    existingAsignacion.FechaInicio = asignacionTurno.FechaInicio;
                    existingAsignacion.FechaFin = asignacionTurno.FechaFin;

                    // Marcar la asignación como modificada
                    _farmaDbContext.AsignacionTurno.Update(existingAsignacion);
                }
                else
                {
                    return false; // Si no se encontró la asignación, devolver false
                }
            }
            else
            {
                // Si no hay ID, se trata de una nueva asignación, agregarla
                _farmaDbContext.AsignacionTurno.Add(asignacionTurno);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int idAsignacion)
        {
            var asignacion = await _farmaDbContext.AsignacionTurno.FindAsync(idAsignacion);
            if (asignacion != null)
            {
                // Eliminar físicamente la asignación
                _farmaDbContext.AsignacionTurno.Remove(asignacion);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<AsignacionTurno>> GetAllAsync()
        {
            return await _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .OrderBy(a => a.FechaInicio)
                .ThenBy(a => a.IdPersonaNavigation.Nombre)
                .ToListAsync();
        }

        public async Task<AsignacionTurno> GetByIdAsync(int idAsignacion)
        {
            try
            {
                var result = await _farmaDbContext.AsignacionTurno
                    .Include(a => a.IdPersonaNavigation)
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdTurnoNavigation)
                    .FirstOrDefaultAsync(a => a.IdAsignacion == idAsignacion);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró la asignación con ID {idAsignacion}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar la asignación de turno", ex);
            }
        }

        public async Task<MPaginatedResult<AsignacionTurno>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .AsQueryable();

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a =>
                    (a.IdPersonaNavigation != null &&
                     (a.IdPersonaNavigation.Nombre.Contains(searchTerm) ||
                      a.IdPersonaNavigation.Apellido.Contains(searchTerm))) ||
                    (a.IdSucursalNavigation != null &&
                     a.IdSucursalNavigation.NombreSucursal.Contains(searchTerm)) ||
                    (a.IdTurnoNavigation != null &&
                     a.IdTurnoNavigation.NombreTurno.Contains(searchTerm)));
            }

            // Ordenamiento
            query = sortAscending
                ? query.OrderBy(a => a.FechaInicio).ThenBy(a => a.IdPersonaNavigation.Nombre)
                : query.OrderByDescending(a => a.FechaInicio).ThenByDescending(a => a.IdPersonaNavigation.Nombre);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<AsignacionTurno>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<AsignacionTurno>> GetByPersonaAsync(int personaId)
        {
            return await _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .Where(a => a.IdPersona == personaId)
                .OrderBy(a => a.FechaInicio)
                .ToListAsync();
        }

        public async Task<List<AsignacionTurno>> GetBySucursalAsync(int sucursalId)
        {
            return await _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .Where(a => a.IdSucursal == sucursalId)
                .OrderBy(a => a.FechaInicio)
                .ThenBy(a => a.IdPersonaNavigation.Nombre)
                .ToListAsync();
        }

        public async Task<List<AsignacionTurno>> GetByTurnoAsync(int turnoId)
        {
            return await _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .Where(a => a.IdTurno == turnoId)
                .OrderBy(a => a.FechaInicio)
                .ThenBy(a => a.IdPersonaNavigation.Nombre)
                .ToListAsync();
        }

        public async Task<List<AsignacionTurno>> GetByDateRangeAsync(DateOnly fechaInicio, DateOnly fechaFin)
        {
            return await _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .Where(a => a.FechaInicio >= fechaInicio && a.FechaFin <= fechaFin)
                .OrderBy(a => a.FechaInicio)
                .ThenBy(a => a.IdPersonaNavigation.Nombre)
                .ToListAsync();
        }

        public async Task<List<AsignacionTurno>> GetActiveAssignmentsAsync(DateOnly fecha)
        {
            return await _farmaDbContext.AsignacionTurno
                .Include(a => a.IdPersonaNavigation)
                .Include(a => a.IdSucursalNavigation)
                .Include(a => a.IdTurnoNavigation)
                .Where(a => a.FechaInicio <= fecha && a.FechaFin >= fecha)
                .OrderBy(a => a.IdPersonaNavigation.Nombre)
                .ToListAsync();
        }
    }
}