using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Telefono
{
    public int IdTelefono { get; set; }

    public int? NumeroTelefonico { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Persona> Persona { get; set; } = new List<Persona>();

    public virtual ICollection<Proveedor> Proveedor { get; set; } = new List<Proveedor>();

    public virtual ICollection<Sucursal> Sucursal { get; set; } = new List<Sucursal>();
}
