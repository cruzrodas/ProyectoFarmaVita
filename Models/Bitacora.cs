using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Bitacora
{
    public int IdBitacora { get; set; }

    public DateTime? FechaHora { get; set; }

    public string? Modulo { get; set; }

    public string? Responsable { get; set; }

    public string? Actividad { get; set; }

    public int? TipoAcceso { get; set; }

    public int? DireccionIp { get; set; }

    public virtual TipoAcceso? TipoAccesoNavigation { get; set; }
}
