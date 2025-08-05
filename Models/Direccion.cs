using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Direccion
{
    public int IdDireccion { get; set; }

    public string? Direccion1 { get; set; }

    public int? IdMunicipio { get; set; }

    public bool? Activo { get; set; }

    public virtual Municipio? IdMunicipioNavigation { get; set; }

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();

    public virtual ICollection<Proveedor> Proveedor { get; set; } = new List<Proveedor>();

    public virtual ICollection<Sucursal> Sucursal { get; set; } = new List<Sucursal>();
}
