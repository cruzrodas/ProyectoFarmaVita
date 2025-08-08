using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.ProveedorServices
{
    public class SProveedorService : IProveedorService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SProveedorService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Proveedor proveedor)
        {
            if (proveedor.IdProveedor > 0)
            {
                // Buscar el proveedor existente en la base de datos
                var existingProveedor = await _farmaDbContext.Proveedor.FindAsync(proveedor.IdProveedor);

                if (existingProveedor != null)
                {
                    // Actualizar las propiedades existentes
                    existingProveedor.NombreProveedor = proveedor.NombreProveedor;
                    existingProveedor.Email = proveedor.Email;
                    existingProveedor.PersonaContacto = proveedor.PersonaContacto;
                    existingProveedor.IdTelefono = proveedor.IdTelefono;
                    existingProveedor.IdDireccion = proveedor.IdDireccion;
                    existingProveedor.Activo = proveedor.Activo;

                    // Marcar el proveedor como modificado
                    _farmaDbContext.Proveedor.Update(existingProveedor);
                }
                else
                {
                    return false; // Si no se encontró el proveedor, devolver false
                }
            }
            else
            {
                // Asegurar que nuevos proveedores estén activos por defecto
                proveedor.Activo = true;

                // Si no hay ID, se trata de un nuevo proveedor, agregarlo
                _farmaDbContext.Proveedor.Add(proveedor);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int idProveedor)
        {
            var proveedor = await _farmaDbContext.Proveedor
                .Include(p => p.Producto)
                .Include(p => p.OrdenRestablecimiento)
                .FirstOrDefaultAsync(p => p.IdProveedor == idProveedor);

            if (proveedor != null)
            {
                // Verificar si el proveedor tiene productos o órdenes asociadas
                if ((proveedor.Producto != null && proveedor.Producto.Any()) ||
                    (proveedor.OrdenRestablecimiento != null && proveedor.OrdenRestablecimiento.Any()))
                {
                    // Eliminación lógica: cambiar estado a inactivo
                    proveedor.Activo = false;
                    _farmaDbContext.Proveedor.Update(proveedor);
                }
                else
                {
                    // Eliminar físicamente si no tiene dependencias
                    _farmaDbContext.Proveedor.Remove(proveedor);
                }

                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Proveedor>> GetAllAsync()
        {
            return await _farmaDbContext.Proveedor
                .Include(p => p.PersonaContactoNavigation)
                .Include(p => p.IdDireccionNavigation)
                    .ThenInclude(d => d.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(p => p.IdTelefonoNavigation)
                .Include(p => p.Producto)
                .OrderBy(p => p.NombreProveedor)
                .ToListAsync();
        }

        public async Task<List<Proveedor>> GetActiveAsync()
        {
            return await _farmaDbContext.Proveedor
                .Include(p => p.PersonaContactoNavigation)
                .Include(p => p.IdDireccionNavigation)
                    .ThenInclude(d => d.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(p => p.IdTelefonoNavigation)
                .Include(p => p.Producto)
                .Where(p => p.Activo == true)
                .OrderBy(p => p.NombreProveedor)
                .ToListAsync();
        }

        public async Task<Proveedor> GetByIdAsync(int idProveedor)
        {
            try
            {
                var result = await _farmaDbContext.Proveedor
                    .Include(p => p.PersonaContactoNavigation)
                    .Include(p => p.IdDireccionNavigation)
                        .ThenInclude(d => d.IdMunicipioNavigation)
                            .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(p => p.IdTelefonoNavigation)
                    .Include(p => p.Producto)
                    .Include(p => p.OrdenRestablecimiento)
                    .FirstOrDefaultAsync(p => p.IdProveedor == idProveedor);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el proveedor con ID {idProveedor}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el proveedor", ex);
            }
        }

        public async Task<MPaginatedResult<Proveedor>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Proveedor
                .Include(p => p.PersonaContactoNavigation)
                .Include(p => p.IdDireccionNavigation)
                    .ThenInclude(d => d.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(p => p.IdTelefonoNavigation)
                .Include(p => p.Producto)
                .Where(p => p.Activo == true) // Solo proveedores activos
                .AsQueryable();

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.NombreProveedor.Contains(searchTerm) ||
                    (p.Email != null && p.Email.Contains(searchTerm)) ||
                    (p.PersonaContactoNavigation != null &&
                     (p.PersonaContactoNavigation.Nombre.Contains(searchTerm) ||
                      p.PersonaContactoNavigation.Apellido.Contains(searchTerm))) ||
                    (p.IdDireccionNavigation != null &&
                     p.IdDireccionNavigation.Direccion1.Contains(searchTerm)));
            }

            // Ordenamiento basado en el campo NombreProveedor
            query = sortAscending
                ? query.OrderBy(p => p.NombreProveedor).ThenBy(p => p.IdProveedor)
                : query.OrderByDescending(p => p.NombreProveedor).ThenByDescending(p => p.IdProveedor);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Proveedor>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Proveedor>> SearchByNameAsync(string nombreProveedor)
        {
            if (string.IsNullOrEmpty(nombreProveedor))
                return new List<Proveedor>();

            return await _farmaDbContext.Proveedor
                .Include(p => p.PersonaContactoNavigation)
                .Include(p => p.IdDireccionNavigation)
                .Include(p => p.IdTelefonoNavigation)
                .Where(p => p.NombreProveedor.Contains(nombreProveedor) && p.Activo == true)
                .OrderBy(p => p.NombreProveedor)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string nombreProveedor, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(nombreProveedor))
                return false;

            var query = _farmaDbContext.Proveedor
                .Where(p => p.NombreProveedor.ToLower() == nombreProveedor.ToLower());

            // Excluir el ID específico (útil para validaciones en edición)
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.IdProveedor != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetProductCountByProveedorAsync(int idProveedor)
        {
            return await _farmaDbContext.Producto
                .Where(p => p.IdProveedor == idProveedor)
                .CountAsync();
        }

        public async Task<List<Proveedor>> GetByPersonaContactoAsync(int personaContactoId)
        {
            return await _farmaDbContext.Proveedor
                .Include(p => p.PersonaContactoNavigation)
                .Include(p => p.IdDireccionNavigation)
                .Include(p => p.IdTelefonoNavigation)
                .Where(p => p.PersonaContacto == personaContactoId && p.Activo == true)
                .OrderBy(p => p.NombreProveedor)
                .ToListAsync();
        }
    }
}