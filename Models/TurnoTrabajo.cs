using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class TurnoTrabajo
{
    public int IdTurno { get; set; }

    public string? NombreTurno { get; set; }

    public DateTime? HoraInicio { get; set; }

    public DateTime? HoraFin { get; set; }

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<AsignacionTurno> AsignacionTurno { get; set; } = new List<AsignacionTurno>();
}
