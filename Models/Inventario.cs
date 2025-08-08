using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Inventario
{
    public int IdInventario { get; set; }

    public string? NombreInventario { get; set; }

    public int? Cantidad { get; set; }

    public int? StockMinimo { get; set; }

    public int? StockMaximo { get; set; }

    public DateTime? UltimaActualizacion { get; set; }

    public virtual ICollection<InventarioProducto> InventarioProducto { get; set; } = new List<InventarioProducto>();

    public virtual ICollection<Sucursal> Sucursal { get; set; } = new List<Sucursal>();
}
