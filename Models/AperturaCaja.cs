using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class AperturaCaja
{
    public int IdAperturaCaja { get; set; }

    public int? IdCaja { get; set; }

    public int? IdPersona { get; set; }

    public DateTime? FechaApertura { get; set; }

    public double? MontoApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    public double? MontoCierre { get; set; }

    public bool? Activa { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<Factura> Factura { get; set; } = new List<Factura>();

    public virtual Caja? IdCajaNavigation { get; set; }

    public virtual Persona? IdPersonaNavigation { get; set; }
}
