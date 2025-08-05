using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Sucursal
{
    public int IdSucursal { get; set; }

    public string? NombreSucursal { get; set; }

    public string? EmailSucursal { get; set; }

    public int? ResponsableSucursal { get; set; }

    public TimeOnly? HorarioApertura { get; set; }

    public TimeOnly? HorarioCierre { get; set; }

    public bool? Activo { get; set; }

    public int? IdTelefono { get; set; }

    public int? IdInventario { get; set; }

    public int? IdDireccion { get; set; }

    public virtual ICollection<AsignacionTurno> AsignacionTurno { get; set; } = new List<AsignacionTurno>();

    public virtual ICollection<Caja> Caja { get; set; } = new List<Caja>();

    public virtual Direccion? IdDireccionNavigation { get; set; }

    public virtual Inventario? IdInventarioNavigation { get; set; }

    public virtual Telefono? IdTelefonoNavigation { get; set; }

    public virtual ICollection<OrdenRestablecimiento> OrdenRestablecimiento { get; set; } = new List<OrdenRestablecimiento>();

    public virtual Persona? ResponsableSucursalNavigation { get; set; }

    public virtual ICollection<Traslado> TrasladoIdSucursalDestinoNavigation { get; set; } = new List<Traslado>();

    public virtual ICollection<Traslado> TrasladoIdSucursalOrigenNavigation { get; set; } = new List<Traslado>();
}
