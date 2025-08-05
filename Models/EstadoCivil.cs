using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class EstadoCivil
{
    public int IdEstadoCivil { get; set; }

    public string? EstadoCivil1 { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();
}
