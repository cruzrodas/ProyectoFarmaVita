using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.EstadoCivilServices
{
    public class SEstadoCivil : IEstadoCivil
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SEstadoCivil(FarmaDbContext farmaDbContext)
        {
         _farmaDbContext = farmaDbContext;   
        }

        public async Task<bool> AddUpdateAsync(EstadoCivil estadocivil)
        {
            if (estadocivil.IdEstadoCivil > 0)
            {
                // Buscar la feria existente en la base de datos
                var existingestadocivil = await _farmaDbContext.EstadoCivil.FindAsync(estadocivil.IdEstadoCivil);

                if (existingestadocivil != null)
                {
                    // Actualizar las propiedades existentes

                    existingestadocivil.EstadoCivil1 = estadocivil.EstadoCivil1;

                    // Marcar el espacio como modificado
                    _farmaDbContext.EstadoCivil.Update(existingestadocivil);
                }
                else
                {
                    return false; // Si no se encontró el espacio, devolver false
                }
            }
            else
            {
                estadocivil.Activo = true;

                // Si no hay ID, se trata de un nuevo espacio, agregarlo
                _farmaDbContext.EstadoCivil.Add(estadocivil);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int estadocivilId)
        {
            var estado = await _farmaDbContext.EstadoCivil.FindAsync(estadocivilId);
            if (estado != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                estado.Activo = false;

                _farmaDbContext.EstadoCivil.Update(estado);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }


        public async Task<List<EstadoCivil>> GetAllAsync()
        {
            return await _farmaDbContext.EstadoCivil
                .Where(e => e.Activo == true)
                .ToListAsync();
        }



        public async Task<EstadoCivil> GetByIdAsync(int id_estadocivil)
        {
            try
            {
                var result = await _farmaDbContext.EstadoCivil
                    .Where(e => e.Activo == true)
                    .FirstOrDefaultAsync(fa => fa.IdEstadoCivil == id_estadocivil);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el área de feria con ID {id_estadocivil}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el área de feria", ex);
            }
        }

        public async Task<MPaginatedResult<EstadoCivil>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.EstadoCivil
                .Where(fa => fa.Activo == true); // Excluir los eliminados

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(fa => fa.EstadoCivil1.Contains(searchTerm));
            }

            // Ordenamiento basado en el campo descripcion_area de areaid
            query = sortAscending
                ? query.OrderBy(fa => fa.IdEstadoCivil).ThenBy(fa => fa.EstadoCivil1)
                : query.OrderByDescending(fa => fa.IdEstadoCivil).ThenByDescending(fa => fa.EstadoCivil1);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
            .ToListAsync();

            return new MPaginatedResult<EstadoCivil>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
