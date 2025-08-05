using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Factura
{
    public int IdFactura { get; set; }

    public DateTime? FechaVenta { get; set; }

    public double? SubTotal { get; set; }

    public double? Impuestos { get; set; }

    public double? Descuento { get; set; }

    public double? Total { get; set; }

    public int? NumeroFactura { get; set; }

    public string? Observaciones { get; set; }

    public int? IdTipoPago { get; set; }

    public int? IdEstado { get; set; }

    public int? IdAperturaCaja { get; set; }

    public int? IdDetalleFactura { get; set; }

    public virtual AperturaCaja? IdAperturaCajaNavigation { get; set; }

    public virtual FacturaDetalle? IdDetalleFacturaNavigation { get; set; }

    public virtual Estado? IdEstadoNavigation { get; set; }

    public virtual TipoPago? IdTipoPagoNavigation { get; set; }
}
