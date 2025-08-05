using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Proveedor
{
    public int IdProveedor { get; set; }

    public string? NombreProveedor { get; set; }

    public int? IdTelefono { get; set; }

    public string? Email { get; set; }

    public int? PersonaContacto { get; set; }

    public int? IdDireccion { get; set; }

    public bool? Activo { get; set; }

    public virtual Direccion? IdDireccionNavigation { get; set; }

    public virtual Telefono? IdTelefonoNavigation { get; set; }

    public virtual ICollection<OrdenRestablecimiento> OrdenRestablecimiento { get; set; } = new List<OrdenRestablecimiento>();

    public virtual Persona? PersonaContactoNavigation { get; set; }

    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
