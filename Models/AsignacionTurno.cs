using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class AsignacionTurno
{
    public int IdAsignacion { get; set; }

    public int? IdPersona { get; set; }

    public int? IdTurno { get; set; }

    public int? IdSucursal { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public virtual Persona? IdPersonaNavigation { get; set; }

    public virtual Sucursal? IdSucursalNavigation { get; set; }

    public virtual TurnoTrabajo? IdTurnoNavigation { get; set; }
}
