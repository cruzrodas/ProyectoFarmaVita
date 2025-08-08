using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.DireccionServices
{
    public class SDireccionService : IDireccionService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SDireccionService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<int> AddAsync(Direccion direccion)
        {
            direccion.Activo = true;
            _farmaDbContext.Direccion.Add(direccion);
            await _farmaDbContext.SaveChangesAsync();
            return direccion.IdDireccion; // Retorna el ID generado
        }

        public async Task<bool> AddUpdateAsync(Direccion direccion)
        {
            if (direccion.IdDireccion > 0)
            {
                // Buscar la dirección existente en la base de datos
                var existingDireccion = await _farmaDbContext.Direccion.FindAsync(direccion.IdDireccion);

                if (existingDireccion != null)
                {
                    // Actualizar las propiedades existentes
                    existingDireccion.Direccion1 = direccion.Direccion1;
                    existingDireccion.IdMunicipio = direccion.IdMunicipio;

                    // Marcar la dirección como modificada
                    _farmaDbContext.Direccion.Update(existingDireccion);
                }
                else
                {
                    return false; // Si no se encontró la dirección, devolver false
                }
            }
            else
            {
                direccion.Activo = true;

                // Si no hay ID, se trata de una nueva dirección, agregarla
                _farmaDbContext.Direccion.Add(direccion);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_direccion)
        {
            var direccion = await _farmaDbContext.Direccion.FindAsync(id_direccion);
            if (direccion != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                direccion.Activo = false;

                _farmaDbContext.Direccion.Update(direccion);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Direccion>> GetAllAsync()
        {
            return await _farmaDbContext.Direccion
                .Include(d => d.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Where(d => d.Activo == true)
                .ToListAsync();
        }

        public async Task<Direccion> GetByIdAsync(int id_direccion)
        {
            try
            {
                var result = await _farmaDbContext.Direccion
                    .Include(d => d.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Where(d => d.Activo == true)
                    .FirstOrDefaultAsync(d => d.IdDireccion == id_direccion);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró la dirección con ID {id_direccion}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar la dirección", ex);
            }
        }

        public async Task<MPaginatedResult<Direccion>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Direccion
                .Include(d => d.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Where(d => d.Activo == true); // Excluir las eliminadas

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Direccion1.Contains(searchTerm) ||
                                       d.IdMunicipioNavigation.NombreMunicipio.Contains(searchTerm) ||
                                       d.IdMunicipioNavigation.IdDepartamentoNavigation.NombreDepartamento.Contains(searchTerm));
            }

            // Ordenamiento basado en el campo Direccion1
            query = sortAscending
                ? query.OrderBy(d => d.IdDireccion).ThenBy(d => d.Direccion1)
                : query.OrderByDescending(d => d.IdDireccion).ThenByDescending(d => d.Direccion1);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Direccion>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Direccion>> GetByMunicipioIdAsync(int municipioId)
        {
            return await _farmaDbContext.Direccion
                .Include(d => d.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Where(d => d.Activo == true && d.IdMunicipio == municipioId)
                .OrderBy(d => d.Direccion1)
                .ToListAsync();
        }
    }
}