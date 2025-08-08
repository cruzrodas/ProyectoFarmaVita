using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.CategoriaProductoService;

namespace ProyectoFarmaVita.Services.CategoriaServices
{
    public class SCategoriaService : ICategoriaService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SCategoriaService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Categoria categoria)
        {
            if (categoria.IdCategoria > 0)
            {
                // Buscar la categoría existente en la base de datos
                var existingCategoria = await _farmaDbContext.Categoria.FindAsync(categoria.IdCategoria);

                if (existingCategoria != null)
                {
                    // Actualizar las propiedades existentes
                    existingCategoria.NombreCategoria = categoria.NombreCategoria;
                    existingCategoria.DescripcionCategoria = categoria.DescripcionCategoria;

                    // Marcar la categoría como modificada
                    _farmaDbContext.Categoria.Update(existingCategoria);
                }
                else
                {
                    return false; // Si no se encontró la categoría, devolver false
                }
            }
            else
            {
                // Si no hay ID, se trata de una nueva categoría, agregarla
                _farmaDbContext.Categoria.Add(categoria);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int idCategoria)
        {
            var categoria = await _farmaDbContext.Categoria
                .Include(c => c.Producto)
                .FirstOrDefaultAsync(c => c.IdCategoria == idCategoria);

            if (categoria != null)
            {
                // Verificar si la categoría tiene productos asociados
                if (categoria.Producto != null && categoria.Producto.Any())
                {
                    throw new InvalidOperationException("No se puede eliminar la categoría porque tiene productos asociados.");
                }

                // Eliminar físicamente la categoría si no tiene productos
                _farmaDbContext.Categoria.Remove(categoria);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Categoria>> GetAllAsync()
        {
            return await _farmaDbContext.Categoria
                .Include(c => c.Producto)
                .OrderBy(c => c.NombreCategoria)
                .ToListAsync();
        }

        public async Task<Categoria> GetByIdAsync(int idCategoria)
        {
            try
            {
                var result = await _farmaDbContext.Categoria
                    .Include(c => c.Producto)
                    .FirstOrDefaultAsync(c => c.IdCategoria == idCategoria);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró la categoría con ID {idCategoria}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar la categoría", ex);
            }
        }

        public async Task<MPaginatedResult<Categoria>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Categoria
                .Include(c => c.Producto)
                .AsQueryable();

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.NombreCategoria.Contains(searchTerm) ||
                    (c.DescripcionCategoria != null && c.DescripcionCategoria.Contains(searchTerm)));
            }

            // Ordenamiento basado en el campo NombreCategoria
            query = sortAscending
                ? query.OrderBy(c => c.NombreCategoria).ThenBy(c => c.IdCategoria)
                : query.OrderByDescending(c => c.NombreCategoria).ThenByDescending(c => c.IdCategoria);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Categoria>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Categoria>> SearchByNameAsync(string nombreCategoria)
        {
            if (string.IsNullOrEmpty(nombreCategoria))
                return new List<Categoria>();

            return await _farmaDbContext.Categoria
                .Include(c => c.Producto)
                .Where(c => c.NombreCategoria.Contains(nombreCategoria))
                .OrderBy(c => c.NombreCategoria)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string nombreCategoria, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(nombreCategoria))
                return false;

            var query = _farmaDbContext.Categoria
                .Where(c => c.NombreCategoria.ToLower() == nombreCategoria.ToLower());

            // Excluir el ID específico (útil para validaciones en edición)
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.IdCategoria != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetProductCountByCategoriaAsync(int idCategoria)
        {
            return await _farmaDbContext.Producto
                .Where(p => p.IdCategoria == idCategoria)
                .CountAsync();
        }
    }
}