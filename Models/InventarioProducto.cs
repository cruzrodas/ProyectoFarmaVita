using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class InventarioProducto
{
    public int IdInventarioProducto { get; set; }

    public int? IdInventario { get; set; }

    public int? IdProducto { get; set; }

    public long? Cantidad { get; set; }

    public long? StockMaximo { get; set; }

    public long? StockMinimo { get; set; }

    public virtual Inventario? IdInventarioNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
