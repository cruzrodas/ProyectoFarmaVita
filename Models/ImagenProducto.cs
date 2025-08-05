using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class ImagenProducto
{
    public int IdImagen { get; set; }

    public int? IdProducto { get; set; }

    public string? Imagen { get; set; }

    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
