using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string? NombreProducto { get; set; }

    public string? DescrpcionProducto { get; set; }

    public double? PrecioVenta { get; set; }

    public double? PrecioCompra { get; set; }

    public bool? RequiereReceta { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public int? IdImagen { get; set; }

    public bool? Activo { get; set; }

    public int? IdCategoria { get; set; }

    public string? UnidadMedida { get; set; }

    public int? IdProveedor { get; set; }

    public int? NivelReorden { get; set; }

    public bool? MedicamentoControlado { get; set; }

    public int? CantidadMaxima { get; set; }

    public DateOnly? FechaCompra { get; set; }

    public int? IdInventario { get; set; }

    public virtual ICollection<DetalleOrdenRes> DetalleOrdenRes { get; set; } = new List<DetalleOrdenRes>();

    public virtual ICollection<FacturaDetalle> FacturaDetalle { get; set; } = new List<FacturaDetalle>();

    public virtual Categoria? IdCategoriaNavigation { get; set; }

    public virtual ImagenProducto? IdImagenNavigation { get; set; }

    public virtual Inventario? IdInventarioNavigation { get; set; }

    public virtual Proveedor? IdProveedorNavigation { get; set; }

    public virtual ICollection<TrasladoDetalle> TrasladoDetalle { get; set; } = new List<TrasladoDetalle>();
}
