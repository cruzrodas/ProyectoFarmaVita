using Microsoft.EntityFrameworkCore;
using ProyectoFarmaVita.Models;
using ProyectoFarmaVita.Services.ProductoService;

namespace ProyectoFarmaVita.Services.ProductoServices
{
    public class SProductoService : IProductoService
    {
        private readonly FarmaDbContext _farmaDbContext;

        public SProductoService(FarmaDbContext farmaDbContext)
        {
            _farmaDbContext = farmaDbContext;
        }

        public async Task<bool> AddUpdateAsync(Producto producto)
        {
            try
            {
                if (producto.IdProducto > 0)
                {
                    // Buscar el producto existente en la base de datos
                    var existingProducto = await _farmaDbContext.Producto
                        .Include(p => p.IdImagenNavigation)
                        .FirstOrDefaultAsync(p => p.IdProducto == producto.IdProducto);

                    if (existingProducto != null)
                    {
                        // Actualizar las propiedades existentes
                        existingProducto.NombreProducto = producto.NombreProducto;
                        existingProducto.DescrpcionProducto = producto.DescrpcionProducto;
                        existingProducto.PrecioVenta = producto.PrecioVenta;
                        existingProducto.PrecioCompra = producto.PrecioCompra;
                        existingProducto.RequiereReceta = producto.RequiereReceta;
                        existingProducto.FechaVencimiento = producto.FechaVencimiento;
                        existingProducto.IdCategoria = producto.IdCategoria;
                        existingProducto.UnidadMedida = producto.UnidadMedida;
                        existingProducto.IdProveedor = producto.IdProveedor;
                        existingProducto.NivelReorden = producto.NivelReorden;
                        existingProducto.MedicamentoControlado = producto.MedicamentoControlado;
                        existingProducto.CantidadMaxima = producto.CantidadMaxima;
                        existingProducto.FechaCompra = producto.FechaCompra;
                        existingProducto.Activo = producto.Activo;

                        // Manejar imagen
                        await HandleImageUpdate(existingProducto, producto);

                        // Marcar el producto como modificado
                        _farmaDbContext.Producto.Update(existingProducto);
                    }
                    else
                    {
                        return false; // Si no se encontró el producto, devolver false
                    }
                }
                else
                {
                    // Asegurar que nuevos productos estén activos por defecto
                    producto.Activo = true;

                    // Manejar imagen para nuevo producto
                    await HandleImageForNewProduct(producto);

                    // Si no hay ID, se trata de un nuevo producto, agregarlo
                    _farmaDbContext.Producto.Add(producto);
                }

                // Guardar los cambios en la base de datos
                await _farmaDbContext.SaveChangesAsync();
                return true; // Retornar true si se ha agregado o actualizado correctamente
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en AddUpdateAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task HandleImageUpdate(Producto existingProducto, Producto newProducto)
        {
            if (newProducto.IdImagenNavigation != null && !string.IsNullOrEmpty(newProducto.IdImagenNavigation.Imagen))
            {
                // Si hay una nueva imagen
                if (existingProducto.IdImagenNavigation != null)
                {
                    // Actualizar imagen existente
                    existingProducto.IdImagenNavigation.Imagen = newProducto.IdImagenNavigation.Imagen;
                    _farmaDbContext.ImagenProducto.Update(existingProducto.IdImagenNavigation);
                }
                else
                {
                    // Crear nueva imagen
                    var nuevaImagen = new ImagenProducto
                    {
                        Imagen = newProducto.IdImagenNavigation.Imagen,
                        IdProducto = existingProducto.IdProducto
                    };
                    _farmaDbContext.ImagenProducto.Add(nuevaImagen);
                    await _farmaDbContext.SaveChangesAsync(); // Guardar para obtener el ID
                    existingProducto.IdImagen = nuevaImagen.IdImagen;
                }
            }
            else if (newProducto.IdImagen == null && existingProducto.IdImagenNavigation != null)
            {
                // Si se quiere quitar la imagen
                _farmaDbContext.ImagenProducto.Remove(existingProducto.IdImagenNavigation);
                existingProducto.IdImagen = null;
            }
        }

        private async Task HandleImageForNewProduct(Producto producto)
        {
            if (producto.IdImagenNavigation != null && !string.IsNullOrEmpty(producto.IdImagenNavigation.Imagen))
            {
                var nuevaImagen = new ImagenProducto
                {
                    Imagen = producto.IdImagenNavigation.Imagen
                };
                _farmaDbContext.ImagenProducto.Add(nuevaImagen);
                await _farmaDbContext.SaveChangesAsync(); // Guardar para obtener el ID
                producto.IdImagen = nuevaImagen.IdImagen;
            }
        }

        public async Task<bool> DeleteAsync(int idProducto)
        {
            var producto = await _farmaDbContext.Producto
                .Include(p => p.FacturaDetalle)
                .Include(p => p.DetalleOrdenRes)
                .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

            if (producto != null)
            {
                // Verificar si el producto tiene dependencias
                bool hasDependencies = (producto.FacturaDetalle != null && producto.FacturaDetalle.Any()) ||
                                     (producto.DetalleOrdenRes != null && producto.DetalleOrdenRes.Any());

                // También verificar si hay inventarios que referencian este producto
                var hasInventoryDependencies = await _farmaDbContext.InventarioProducto
                    .AnyAsync(ip => ip.IdProducto == idProducto);

                if (hasDependencies || hasInventoryDependencies)
                {
                    // Eliminación lógica: cambiar estado a inactivo
                    producto.Activo = false;
                    _farmaDbContext.Producto.Update(producto);
                }
                else
                {
                    // Eliminar físicamente si no tiene dependencias
                    _farmaDbContext.Producto.Remove(producto);
                }

                await _farmaDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Producto>> GetAllAsync()
        {
            return await _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Include(p => p.IdImagenNavigation)
                .OrderBy(p => p.NombreProducto)
                .ToListAsync();
        }

        public async Task<List<Producto>> GetActivosAsync()
        {
            return await _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Include(p => p.IdImagenNavigation)
                .Where(p => p.Activo == true)
                .OrderBy(p => p.NombreProducto)
                .ToListAsync();
        }

        public async Task<Producto> GetByIdAsync(int idProducto)
        {
            try
            {
                var result = await _farmaDbContext.Producto
                    .Include(p => p.IdCategoriaNavigation)
                    .Include(p => p.IdProveedorNavigation)
                    .Include(p => p.IdImagenNavigation)
                    .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

                if (result == null)
                {
                    throw new KeyNotFoundException($"No se encontró el producto con ID {idProducto}");
                }

                // Debug: verificar si se carga la imagen
                Console.WriteLine($"Producto cargado: {result.NombreProducto}");
                Console.WriteLine($"Tiene imagen: {result.IdImagenNavigation != null}");
                if (result.IdImagenNavigation != null)
                {
                    Console.WriteLine($"Longitud imagen: {result.IdImagenNavigation.Imagen?.Length ?? 0}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al recuperar producto: {ex.Message}");
                throw new Exception("Error al recuperar el producto", ex);
            }
        }

        public async Task<MPaginatedResult<Producto>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true)
        {
            var query = _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Include(p => p.IdImagenNavigation)
                .Where(p => p.Activo == true) // Solo productos activos
                .AsQueryable();

            // Filtro por el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.NombreProducto.Contains(searchTerm) ||
                    (p.DescrpcionProducto != null && p.DescrpcionProducto.Contains(searchTerm)) ||
                    (p.IdCategoriaNavigation != null && p.IdCategoriaNavigation.NombreCategoria.Contains(searchTerm)) ||
                    (p.IdProveedorNavigation != null && p.IdProveedorNavigation.NombreProveedor.Contains(searchTerm)));
            }

            // Ordenamiento
            query = sortAscending
                ? query.OrderBy(p => p.NombreProducto).ThenBy(p => p.IdProducto)
                : query.OrderByDescending(p => p.NombreProducto).ThenByDescending(p => p.IdProducto);

            var totalItems = await query.CountAsync();

            // Aplicar paginación
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MPaginatedResult<Producto>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<Producto>> GetByCategoriaAsync(int categoriaId)
        {
            return await _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Include(p => p.IdImagenNavigation)
                .Where(p => p.IdCategoria == categoriaId && p.Activo == true)
                .OrderBy(p => p.NombreProducto)
                .ToListAsync();
        }

        public async Task<List<Producto>> GetByProveedorAsync(int proveedorId)
        {
            return await _farmaDbContext.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdProveedorNavigation)
                .Include(p => p.IdImagenNavigation)
                .Where(p => p.IdProveedor == proveedorId && p.Activo == true)
                .OrderBy(p => p.NombreProducto)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string nombreProducto, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(nombreProducto))
                return false;

            var query = _farmaDbContext.Producto
                .Where(p => p.NombreProducto.ToLower() == nombreProducto.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.IdProducto != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}