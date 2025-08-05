using System;
using System.Collections.Generic;

namespace ProyectoFarmaVita.Models;

public partial class TipoAcceso
{
    public int IdTipoAcceso { get; set; }

    public string? NombreAcceso { get; set; }

    public virtual ICollection<Bitacora> Bitacora { get; set; } = new List<Bitacora>();
}
