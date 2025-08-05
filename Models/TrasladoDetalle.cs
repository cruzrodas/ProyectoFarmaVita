using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class TrasladoDetalle
{
    public int IdTrasladoDetalle { get; set; }

    public int? IdProducto { get; set; }

    public int? Cantidad { get; set; }

    public int? IdEstado { get; set; }

    public virtual Estado? IdEstadoNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }

    public virtual ICollection<Traslado> Traslado { get; set; } = new List<Traslado>();
}
