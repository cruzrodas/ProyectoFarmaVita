using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Estado
{
    public int IdEstado { get; set; }

    public string? Estado1 { get; set; }

    public virtual ICollection<Factura> Factura { get; set; } = new List<Factura>();

    public virtual ICollection<OrdenRestablecimiento> OrdenRestablecimiento { get; set; } = new List<OrdenRestablecimiento>();

    public virtual ICollection<Traslado> Traslado { get; set; } = new List<Traslado>();

    public virtual ICollection<TrasladoDetalle> TrasladoDetalle { get; set; } = new List<TrasladoDetalle>();
}
