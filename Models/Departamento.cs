using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Departamento
{
    public int IdDepartamento { get; set; }

    public string? NombreDepartamento { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Municipio> Municipio { get; set; } = new List<Municipio>();
}
