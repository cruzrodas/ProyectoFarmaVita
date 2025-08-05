using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Genero
{
    public int IdGenero { get; set; }

    public string? Ngenero { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();
}
