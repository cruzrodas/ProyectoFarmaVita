using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class DetalleOrdenRes
{
    public int IdDetalle { get; set; }

    public int? IdProducto { get; set; }

    public int? CantidadSolicitada { get; set; }

    public double? PrecioUnitario { get; set; }

    public double? Subtotal { get; set; }

    public int? Total { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }

    public virtual ICollection<OrdenRestablecimiento> OrdenRestablecimiento { get; set; } = new List<OrdenRestablecimiento>();
}
