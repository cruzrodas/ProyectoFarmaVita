using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.SucursalServices
{
    public class SSucursalService : ISucursalService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SSucursalService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Sucursal sucursal)
        {
            if (sucursal.IdSucursal > 0)
            {
                // Buscar la sucursal existente en la base de datos
                var existingSucursal = await _farmaDbContext.Sucursal.FindAsync(sucursal.IdSucursal);

                if (existingSucursal != null)
                {
                    // Actualizar las propiedades existentes
                    existingSucursal.NombreSucursal = sucursal.NombreSucursal;
                    existingSucursal.EmailSucursal = sucursal.EmailSucursal;
                    existingSucursal.ResponsableSucursal = sucursal.ResponsableSucursal;
                    existingSucursal.HorarioApertura = sucursal.HorarioApertura;
                    existingSucursal.HorarioCierre = sucursal.HorarioCierre;
                    existingSucursal.IdTelefono = sucursal.IdTelefono;
                    existingSucursal.IdInventario = sucursal.IdInventario;
                    existingSucursal.IdDireccion = sucursal.IdDireccion;

                    // Marcar la sucursal como modificada
                    _farmaDbContext.Sucursal.Update(existingSucursal);
                }
                else
                {
                    return false; // Si no se encontró la sucursal, devolver false
                }
            }
            else
            {
                sucursal.Activo = true;

                // Si no hay ID, se trata de una nueva sucursal, agregarla
                _farmaDbContext.Sucursal.Add(sucursal);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_sucursal)
        {
            var sucursal = await _farmaDbContext.Sucursal.FindAsync(id_sucursal);
            if (sucursal != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                sucursal.Activo = false;

                _farmaDbContext.Sucursal.Update(sucursal);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Sucursal>> GetAllAsync()
        {
            return await _farmaDbContext.Sucursal
                .Include(s => s.ResponsableSucursalNavigation)
                .Include(s => s.IdDireccionNavigation)
                .Include(s => s.IdTelefonoNavigation)
                .Where(s => s.Activo == true)
                .ToListAsync();
        }

        public async Task<Sucursal> GetByIdAsync(int id_sucursal)
        {
            try
            {
                var result = await _farmaDbContext.Sucursal
                    .Include(s => s.ResponsableSucursalNavigation)
                    .Include(s => s.IdDireccionNavigation)
                    .Include(s => s.IdTelefonoNavigation)
                    .Where(s => s.Activo == true)
                    .FirstOrDefaultAsync(s => s.IdSucursal == id_sucursal);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró la sucursal con ID {id_sucursal}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar la sucursal", ex);
            }
        }

        public async Task<MPaginatedResult<Sucursal>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Sucursal
                .Include(s => s.ResponsableSucursalNavigation)
                .Include(s => s.IdDireccionNavigation)
                .Include(s => s.IdTelefonoNavigation)
                .Where(s => s.Activo == true); // Excluir las eliminadas

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.NombreSucursal.Contains(searchTerm) ||
                                       s.EmailSucursal.Contains(searchTerm) ||
                                       (s.ResponsableSucursalNavigation != null &&
                                        (s.ResponsableSucursalNavigation.Nombre.Contains(searchTerm) ||
                                         s.ResponsableSucursalNavigation.Apellido.Contains(searchTerm))));
            }

            // Ordenamiento basado en el campo NombreSucursal
            query = sortAscending
                ? query.OrderBy(s => s.IdSucursal).ThenBy(s => s.NombreSucursal)
                : query.OrderByDescending(s => s.IdSucursal).ThenByDescending(s => s.NombreSucursal);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Sucursal>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Sucursal>> GetByResponsableAsync(int responsableId)
        {
            return await _farmaDbContext.Sucursal
                .Include(s => s.ResponsableSucursalNavigation)
                .Include(s => s.IdDireccionNavigation)
                .Include(s => s.IdTelefonoNavigation)
                .Where(s => s.Activo == true && s.ResponsableSucursal == responsableId)
                .OrderBy(s => s.NombreSucursal)
                .ToListAsync();
        }
    }
}