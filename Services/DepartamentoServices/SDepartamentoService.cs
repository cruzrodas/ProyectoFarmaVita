using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.DepartamentoServices
{
    public class SDepartamentoService : IdepartamentoService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SDepartamentoService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Departamento departamento)
        {
            if (departamento.IdDepartamento > 0)
            {
                // Buscar el departamento existente en la base de datos
                var existingDepartamento = await _farmaDbContext.Departamento.FindAsync(departamento.IdDepartamento);

                if (existingDepartamento != null)
                {
                    // Actualizar las propiedades existentes
                    existingDepartamento.NombreDepartamento = departamento.NombreDepartamento;

                    // Marcar el departamento como modificado
                    _farmaDbContext.Departamento.Update(existingDepartamento);
                }
                else
                {
                    return false; // Si no se encontró el departamento, devolver false
                }
            }
            else
            {
                departamento.Activo = true;

                // Si no hay ID, se trata de un nuevo departamento, agregarlo
                _farmaDbContext.Departamento.Add(departamento);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_departamento)
        {
            var departamento = await _farmaDbContext.Departamento.FindAsync(id_departamento);
            if (departamento != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                departamento.Activo = false;

                _farmaDbContext.Departamento.Update(departamento);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Departamento>> GetAllAsync()
        {
            return await _farmaDbContext.Departamento
                .Where(d => d.Activo == true)
                .ToListAsync();
        }

        public async Task<Departamento> GetByIdAsync(int id_departamento)
        {
            try
            {
                var result = await _farmaDbContext.Departamento
                    .Where(d => d.Activo == true)
                    .FirstOrDefaultAsync(d => d.IdDepartamento == id_departamento);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el departamento con ID {id_departamento}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el departamento", ex);
            }
        }

        public async Task<MPaginatedResult<Departamento>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Departamento
                .Where(d => d.Activo == true); // Excluir los eliminados

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.NombreDepartamento.Contains(searchTerm));
            }

            // Ordenamiento basado en el campo NombreDepartamento
            query = sortAscending
                ? query.OrderBy(d => d.IdDepartamento).ThenBy(d => d.NombreDepartamento)
                : query.OrderByDescending(d => d.IdDepartamento).ThenByDescending(d => d.NombreDepartamento);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Departamento>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}