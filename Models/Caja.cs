using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Caja
{
    public int IdCaja { get; set; }

    public string? NombreCaja { get; set; }

    public bool? Activa { get; set; }

    public int? IdSucursal { get; set; }

    public virtual ICollection<AperturaCaja> AperturaCaja { get; set; } = new List<AperturaCaja>();

    public virtual Sucursal? IdSucursalNavigation { get; set; }
}
