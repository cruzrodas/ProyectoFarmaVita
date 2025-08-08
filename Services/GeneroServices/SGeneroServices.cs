using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.EstadoCivilServices;

namespace ProyectoFarmaVita.Services.GeneroServices
{
    public class SGeneroServices : IGeneroServices
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SGeneroServices(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Genero genero)
        {
            if (genero.IdGenero > 0)
            {
                // Buscar la feria existente en la base de datos
                var existinggenero = await _farmaDbContext.Genero.FindAsync(genero.IdGenero);

                if (existinggenero != null)
                {
                    // Actualizar las propiedades existentes

                    existinggenero.Ngenero = genero.Ngenero;

                    // Marcar el espacio como modificado
                    _farmaDbContext.Genero.Update(existinggenero);
                }
                else
                {
                    return false; // Si no se encontró el espacio, devolver false
                }
            }
            else
            {
                genero.Activo = true;

                // Si no hay ID, se trata de un nuevo espacio, agregarlo
                _farmaDbContext.Genero.Add(genero);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int genero)
        {
            var estado = await _farmaDbContext.Genero.FindAsync(genero);
            if (estado != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                estado.Activo = false;

                _farmaDbContext.Genero.Update(estado);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Genero>> GetAllAsync()
        {
            return await _farmaDbContext.Genero
             .Where(e => e.Activo == true)
              .ToListAsync();
        }

        public async Task<Genero> GetByIdAsync(int id_genero)
        {

            try
            {
                var result = await _farmaDbContext.Genero
                    .Where(e => e.Activo == true)
                    .FirstOrDefaultAsync(fa => fa.IdGenero == id_genero);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el genero de feria con ID {id_genero}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el Genero", ex);
            }
        }

        public async Task<MPaginatedResult<Genero>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Genero
            .Where(fa => fa.Activo == true); // Excluir los eliminados

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(fa => fa.Ngenero.Contains(searchTerm));
            }

            // Ordenamiento basado en el campo descripcion_area de areaid
            query = sortAscending
                ? query.OrderBy(fa => fa.IdGenero).ThenBy(fa => fa.Ngenero)
                : query.OrderByDescending(fa => fa.IdGenero).ThenByDescending(fa => fa.Ngenero);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
            .ToListAsync();

            return new MPaginatedResult<Genero>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
