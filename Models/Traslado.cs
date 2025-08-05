using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Traslado
{
    public int IdTraslado { get; set; }

    public int? IdSucursalOrigen { get; set; }

    public int? IdSucursalDestino { get; set; }

    public DateTime? FechaTraslado { get; set; }

    public int? IdEstadoTraslado { get; set; }

    public string? Observaciones { get; set; }

    public int? IdTrasladodetalles { get; set; }

    public virtual Estado? IdEstadoTrasladoNavigation { get; set; }

    public virtual Sucursal? IdSucursalDestinoNavigation { get; set; }

    public virtual Sucursal? IdSucursalOrigenNavigation { get; set; }

    public virtual TrasladoDetalle? IdTrasladodetallesNavigation { get; set; }
}
