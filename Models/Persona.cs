using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Persona
{
    public int IdPersona { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public int? Dpi { get; set; }

    public int? Nit { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string? Email { get; set; }

    public string? FechaRegistro { get; set; }

    public double? Salario { get; set; }

    public bool? Activo { get; set; }

    public string? Contraseña { get; set; }

    public int? IdTelefono { get; set; }

    public int? IdSucursal { get; set; }

    public int? IdEstadoCivil { get; set; }

    public int? IdGenero { get; set; }

    public int? IdRool { get; set; }

    public int? IdDireccion { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? UsuarioCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public virtual ICollection<AperturaCaja> AperturaCaja { get; set; } = new List<AperturaCaja>();

    public virtual ICollection<AsignacionTurno> AsignacionTurno { get; set; } = new List<AsignacionTurno>();

    public virtual Direccion? IdDireccionNavigation { get; set; }

    public virtual EstadoCivil? IdEstadoCivilNavigation { get; set; }

    public virtual Genero? IdGeneroNavigation { get; set; }

    public virtual Rol? IdRoolNavigation { get; set; }

    public virtual Telefono? IdTelefonoNavigation { get; set; }

    public virtual ICollection<OrdenRestablecimiento> OrdenRestablecimientoIdPersonaSolicitudNavigation { get; set; } = new List<OrdenRestablecimiento>();

    public virtual ICollection<OrdenRestablecimiento> OrdenRestablecimientoUsuarioAprobacionNavigation { get; set; } = new List<OrdenRestablecimiento>();

    public virtual ICollection<Proveedor> Proveedor { get; set; } = new List<Proveedor>();

    public virtual ICollection<Sucursal> Sucursal { get; set; } = new List<Sucursal>();
}
