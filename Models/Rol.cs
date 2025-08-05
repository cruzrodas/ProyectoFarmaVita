using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Rol
{
    public int IdRol { get; set; }

    public string? TipoRol { get; set; }

    public string? DescripcionRol { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();
}
