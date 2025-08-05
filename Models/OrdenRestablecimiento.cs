using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class OrdenRestablecimiento
{
    public int IdOrden { get; set; }

    public int? IdProveedor { get; set; }

    public DateTime? FechaOrden { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public int? IdEstado { get; set; }

    public int? IdPersonaSolicitud { get; set; }

    public int? IdSucursal { get; set; }

    public double? Total { get; set; }

    public string? Observaciones { get; set; }

    public int? IdDetalleOrden { get; set; }

    public bool? Aprobada { get; set; }

    public int? UsuarioAprobacion { get; set; }

    public virtual DetalleOrdenRes? IdDetalleOrdenNavigation { get; set; }

    public virtual Estado? IdEstadoNavigation { get; set; }

    public virtual Persona? IdPersonaSolicitudNavigation { get; set; }

    public virtual Proveedor? IdProveedorNavigation { get; set; }

    public virtual Sucursal? IdSucursalNavigation { get; set; }

    public virtual Persona? UsuarioAprobacionNavigation { get; set; }
}
