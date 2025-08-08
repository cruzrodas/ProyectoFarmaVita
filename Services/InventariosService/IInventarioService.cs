using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.InventarioService
{
    public interface IInventarioService
    {
        #region Métodos CRUD Básicos
        Task<bool> AddUpdateAsync(Inventario inventario);
        Task<bool> DeleteAsync(int id_inventario);
        Task<List<Inventario>> GetAllAsync();
        Task<Inventario> GetByIdAsync(int id_inventario);
        Task<MPaginatedResult<Inventario>> GetPaginatedAsync(int pageNumber, int pageSize, string searchTerm = "", bool sortAscending = true);
        #endregion

        #region Consultas por Producto
        Task<List<Inventario>> GetByProductoIdAsync(int productoId);
        Task<bool> ExistsForProductAsync(int productoId, string nombreInventario, int? excludeId = null);
        #endregion

        #region Consultas por Estado de Stock
        Task<List<Inventario>> GetLowStockAsync(); // Inventarios con stock bajo
        Task<List<Inventario>> GetOutOfStockAsync(); // Inventarios sin stock
        Task<List<Inventario>> GetHighStockAsync(); // Inventarios con stock alto
        #endregion

        #region Gestión de Stock
        /// <summary>
        /// Ajusta la cantidad de stock de un inventario (suma o resta)
        /// </summary>
        /// <param name="inventarioId">ID del inventario</param>
        /// <param name="adjustment">Cantidad a ajustar (positiva para sumar, negativa para restar)</param>
        /// <param name="reason">Motivo del ajuste</param>
        /// <returns>True si se realizó el ajuste correctamente</returns>
        Task<bool> AdjustStockAsync(int inventarioId, int adjustment, string reason = "");

        /// <summary>
        /// Establece una cantidad específica de stock
        /// </summary>
        /// <param name="inventarioId">ID del inventario</param>
        /// <param name="newQuantity">Nueva cantidad</param>
        /// <param name="reason">Motivo del cambio</param>
        /// <returns>True si se estableció la cantidad correctamente</returns>
        Task<bool> SetStockAsync(int inventarioId, int newQuantity, string reason = "");
        #endregion

        #region Consultas por Sucursal
        /// <summary>
        /// Obtiene inventarios asociados a una sucursal específica
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Lista de inventarios de la sucursal</returns>
        Task<List<Inventario>> GetInventariosBySucursalAsync(int sucursalId);
        #endregion

        #region Estadísticas y Reportes
        /// <summary>
        /// Obtiene estadísticas generales del inventario
        /// </summary>
        /// <returns>Diccionario con estadísticas clave</returns>
        Task<Dictionary<string, int>> GetInventoryStatisticsAsync();

        /// <summary>
        /// Obtiene inventarios de productos próximos a vencer
        /// </summary>
        /// <param name="days">Días de anticipación para considerar próximo a vencer</param>
        /// <returns>Lista de inventarios con productos próximos a vencer</returns>
        Task<List<Inventario>> GetInventariosExpiringSoonAsync(int days = 30);

        /// <summary>
        /// Obtiene los inventarios con mayor movimiento (por cantidad)
        /// </summary>
        /// <param name="top">Número de inventarios a retornar</param>
        /// <returns>Lista de inventarios con mayor stock</returns>
        Task<List<Inventario>> GetTopMovedInventariosAsync(int top = 10);
        #endregion
    }
}