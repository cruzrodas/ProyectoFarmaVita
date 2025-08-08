using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.TelefonoServices
{
    public class STelefonoService : ITelefonoService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public STelefonoService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<int> AddAsync(Telefono telefono)
        {
            telefono.Activo = true;
            _farmaDbContext.Telefono.Add(telefono);
            await _farmaDbContext.SaveChangesAsync();
            return telefono.IdTelefono; // Retorna el ID generado
        }

        public async Task<bool> AddUpdateAsync(Telefono telefono)
        {
            if (telefono.IdTelefono > 0)
            {
                // Buscar el teléfono existente en la base de datos
                var existingTelefono = await _farmaDbContext.Telefono.FindAsync(telefono.IdTelefono);

                if (existingTelefono != null)
                {
                    // Actualizar las propiedades existentes
                    existingTelefono.NumeroTelefonico = telefono.NumeroTelefonico;

                    // Marcar el teléfono como modificado
                    _farmaDbContext.Telefono.Update(existingTelefono);
                }
                else
                {
                    return false; // Si no se encontró el teléfono, devolver false
                }
            }
            else
            {
                telefono.Activo = true;

                // Si no hay ID, se trata de un nuevo teléfono, agregarlo
                _farmaDbContext.Telefono.Add(telefono);
            }

            // Guardar los cambios en la base de datos
            await _farmaDbContext.SaveChangesAsync();
            return true; // Retornar true si se ha agregado o actualizado correctamente
        }

        public async Task<bool> DeleteAsync(int id_telefono)
        {
            var telefono = await _farmaDbContext.Telefono.FindAsync(id_telefono);
            if (telefono != null)
            {
                // Eliminar lógicamente: cambiar estado a inactivo
                telefono.Activo = false;

                _farmaDbContext.Telefono.Update(telefono);
                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Telefono>> GetAllAsync()
        {
            return await _farmaDbContext.Telefono
                .Where(t => t.Activo == true)
                .ToListAsync();
        }

        public async Task<Telefono> GetByIdAsync(int id_telefono)
        {
            try
            {
                var result = await _farmaDbContext.Telefono
                    .Where(t => t.Activo == true)
                    .FirstOrDefaultAsync(t => t.IdTelefono == id_telefono);

                if (result == null)
                {
                    // Manejar el caso donde no se encontró el objeto
                    throw new KeyNotFoundException($"No se encontró el teléfono con ID {id_telefono}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al recuperar el teléfono", ex);
            }
        }

        public async Task<MPaginatedResult<Telefono>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Telefono
                .Where(t => t.Activo == true); // Excluir los eliminados

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.NumeroTelefonico.ToString().Contains(searchTerm));
            }

            // Ordenamiento basado en el campo NumeroTelefonico
            query = sortAscending
                ? query.OrderBy(t => t.IdTelefono).ThenBy(t => t.NumeroTelefonico)
                : query.OrderByDescending(t => t.IdTelefono).ThenByDescending(t => t.NumeroTelefonico);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Telefono>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}