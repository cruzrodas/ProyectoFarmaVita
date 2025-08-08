using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.MunicipioService;

namespace ProyectoFarmaVita.Services.MunicipioServices
{
    public class SMunicipioService : IMunicipioService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SMunicipioService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Municipio municipio)
        {
            if (municipio.IdMunicipio > 0)
            {
                // Buscar el municipio existente en la base de datos
                var existingMunicipio = await _farmaDbContext.Municipio.FindAsync(municipio.IdMunicipio);

                if (existingMunicipio != null)
                {
                    // Actualizar las propiedades existentes
                    existingMunicipio.NombreMunicipio = municipio.NombreMunicipio;
                    existingMunicipio.IdDepartamento = municipio.IdDepartamento;

                    // Marcar el municipio como modificado
                    _farmaDbContext.Municipio.Update(existingMunicipio);
                }
                else
                {
                    return false; // Si no se encontró el municipio, devolver false
                }
            }
            else
            {
                municipio.Activo = true;

                // Si no hay ID, se trata de un nuevo municipio, agregarlo
                _farmaDbContext.Municipio.Add(municipio);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_municipio)
        {
            var municipio = await _farmaDbContext.Municipio.FindAsync(id_municipio);
            if (municipio != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                municipio.Activo = false;

                _farmaDbContext.Municipio.Update(municipio);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Municipio>> GetAllAsync()
        {
            return await _farmaDbContext.Municipio
                .Include(m => m.IdDepartamentoNavigation)
                .Where(m => m.Activo == true)
                .ToListAsync();
        }

        public async Task<Municipio> GetByIdAsync(int id_municipio)
        {
            try
            {
                var result = await _farmaDbContext.Municipio
                    .Include(m => m.IdDepartamentoNavigation)
                    .Where(m => m.Activo == true)
                    .FirstOrDefaultAsync(m => m.IdMunicipio == id_municipio);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el municipio con ID {id_municipio}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el municipio", ex);
            }
        }

        public async Task<MPaginatedResult<Municipio>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Municipio
                .Include(m => m.IdDepartamentoNavigation)
                .Where(m => m.Activo == true); // Excluir los eliminados

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.NombreMunicipio.Contains(searchTerm) ||
                                       m.IdDepartamentoNavigation.NombreDepartamento.Contains(searchTerm));
            }

            // Ordenamiento basado en el campo NombreMunicipio
            query = sortAscending
                ? query.OrderBy(m => m.IdMunicipio).ThenBy(m => m.NombreMunicipio)
                : query.OrderByDescending(m => m.IdMunicipio).ThenByDescending(m => m.NombreMunicipio);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Municipio>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Municipio>> GetByDepartamentoIdAsync(int departamentoId)
        {
            return await _farmaDbContext.Municipio
                .Where(m => m.Activo == true && m.IdDepartamento == departamentoId)
                .OrderBy(m => m.NombreMunicipio)
                .ToListAsync();
        }
    }
}