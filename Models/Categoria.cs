using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class Categoria
{
    public int IdCategoria { get; set; }

    public string? NombreCategoria { get; set; }

    public string? DescripcionCategoria { get; set; }

    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
