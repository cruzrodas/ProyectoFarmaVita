using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class TipoPago
{
    public int IdTipoPago { get; set; }

    public string? NombrePago { get; set; }

    public virtual ICollection<Factura> Factura { get; set; } = new List<Factura>();
}
