using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Municipio
{
    public int IdMunicipio { get; set; }

    public string? NombreMunicipio { get; set; }

    public int? IdDepartamento { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Direccion> Direccion { get; set; } = new List<Direccion>();

    public virtual Departamento? IdDepartamentoNavigation { get; set; }
}
