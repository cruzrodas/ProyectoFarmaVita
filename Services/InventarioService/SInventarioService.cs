using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.InventarioServices
{
    public class SInventarioService : IInventarioService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SInventarioService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Inventario inventario)
        {
            if (inventario.IdInventario > 0)
            {
                // Buscar el inventario existente en la base de datos
                var existingInventario = await _farmaDbContext.Inventario.FindAsync(inventario.IdInventario);

                if (existingInventario != null)
                {
                    // Actualizar las propiedades existentes
                    existingInventario.IdProducto = inventario.IdProducto;
                    existingInventario.NombreInventario = inventario.NombreInventario;
                    existingInventario.Cantidad = inventario.Cantidad;
                    existingInventario.StockMinimo = inventario.StockMinimo;
                    existingInventario.StockMaximo = inventario.StockMaximo;
                    existingInventario.UltimaActualizacion = DateTime.Now;

                    // Marcar el inventario como modificado
                    _farmaDbContext.Inventario.Update(existingInventario);
                }
                else
                {
                    return false; // Si no se encontró el inventario, devolver false
                }
            }
            else
            {
                inventario.UltimaActualizacion = DateTime.Now;

                // Si no hay ID, se trata de un nuevo inventario, agregarlo
                _farmaDbContext.Inventario.Add(inventario);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_inventario)
        {
            var inventario = await _farmaDbContext.Inventario.FindAsync(id_inventario);
            if (inventario != null)
            {
                // Eliminar físicamente el inventario (no tiene campo Activo)
                _farmaDbContext.Inventario.Remove(inventario);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Inventario>> GetAllAsync()
        {
            return await _farmaDbContext.Inventario
                .Include(i => i.IdProductoNavigation)
                .ToListAsync();
        }

        public async Task<Inventario> GetByIdAsync(int id_inventario)
        {
            try
            {
                var result = await _farmaDbContext.Inventario
                    .Include(i => i.IdProductoNavigation)
                    .FirstOrDefaultAsync(i => i.IdInventario == id_inventario);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el inventario con ID {id_inventario}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el inventario", ex);
            }
        }

        public async Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Inventario
                .Include(i => i.IdProductoNavigation)
                .AsQueryable();

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i => i.NombreInventario.Contains(searchTerm) ||
                                       (i.IdProductoNavigation != null && i.IdProductoNavigation.NombreProducto.Contains(searchTerm)));
            }

            // Ordenamiento basado en el campo NombreInventario
            query = sortAscending
                ? query.OrderBy(i => i.IdInventario).ThenBy(i => i.NombreInventario)
                : query.OrderByDescending(i => i.IdInventario).ThenByDescending(i => i.NombreInventario);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Inventario>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Inventario>> GetByProductoIdAsync(int productoId)
        {
            return await _farmaDbContext.Inventario
                .Include(i => i.IdProductoNavigation)
                .Where(i => i.IdProducto == productoId)
                .OrderBy(i => i.NombreInventario)
                .ToListAsync();
        }

        public async Task<List<Inventario>> GetLowStockAsync()
        {
            return await _farmaDbContext.Inventario
                .Include(i => i.IdProductoNavigation)
                .Where(i => i.Cantidad <= i.StockMinimo)
                .OrderBy(i => i.Cantidad)
                .ToListAsync();
        }
    }
}