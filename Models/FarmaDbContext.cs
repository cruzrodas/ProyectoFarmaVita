using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProyectoFarmaVita.Models;

public partial class FarmaDbContext : DbContext
{
    public FarmaDbContext()
    {
    }

    public FarmaDbContext(DbContextOptions<FarmaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AperturaCaja> AperturaCaja { get; set; }

    public virtual DbSet<AsignacionTurno> AsignacionTurno { get; set; }

    public virtual DbSet<Bitacora> Bitacora { get; set; }

    public virtual DbSet<Caja> Caja { get; set; }

    public virtual DbSet<Categoria> Categoria { get; set; }

    public virtual DbSet<Departamento> Departamento { get; set; }

    public virtual DbSet<DetalleOrdenRes> DetalleOrdenRes { get; set; }

    public virtual DbSet<Direccion> Direccion { get; set; }

    public virtual DbSet<Estado> Estado { get; set; }

    public virtual DbSet<EstadoCivil> EstadoCivil { get; set; }

    public virtual DbSet<Factura> Factura { get; set; }

    public virtual DbSet<FacturaDetalle> FacturaDetalle { get; set; }

    public virtual DbSet<Genero> Genero { get; set; }

    public virtual DbSet<ImagenProducto> ImagenProducto { get; set; }

    public virtual DbSet<Inventario> Inventario { get; set; }

    public virtual DbSet<Municipio> Municipio { get; set; }

    public virtual DbSet<OrdenRestablecimiento> OrdenRestablecimiento { get; set; }

    public virtual DbSet<Persona> Persona { get; set; }

    public virtual DbSet<Producto> Producto { get; set; }

    public virtual DbSet<Proveedor> Proveedor { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<Sucursal> Sucursal { get; set; }

    public virtual DbSet<Telefono> Telefono { get; set; }

    public virtual DbSet<TipoAcceso> TipoAcceso { get; set; }

    public virtual DbSet<TipoPago> TipoPago { get; set; }

    public virtual DbSet<Traslado> Traslado { get; set; }

    public virtual DbSet<TrasladoDetalle> TrasladoDetalle { get; set; }

    public virtual DbSet<TurnoTrabajo> TurnoTrabajo { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AperturaCaja>(entity =>
        {
            entity.HasKey(e => e.IdAperturaCaja);

            entity.Property(e => e.IdAperturaCaja).HasColumnName("Id_AperturaCaja");
            entity.Property(e => e.FechaApertura)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Apertura");
            entity.Property(e => e.FechaCierre)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Cierre");
            entity.Property(e => e.IdCaja).HasColumnName("Id_Caja");
            entity.Property(e => e.IdPersona).HasColumnName("Id_Persona");
            entity.Property(e => e.MontoApertura).HasColumnName("Monto_Apertura");
            entity.Property(e => e.MontoCierre).HasColumnName("Monto_Cierre");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.IdCajaNavigation).WithMany(p => p.AperturaCaja)
                .HasForeignKey(d => d.IdCaja)
                .HasConstraintName("FK_AperturaCaja_Caja");

            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.AperturaCaja)
                .HasForeignKey(d => d.IdPersona)
                .HasConstraintName("FK_AperturaCaja_Persona");
        });

        modelBuilder.Entity<AsignacionTurno>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion);

            entity.ToTable("Asignacion_Turno");

            entity.Property(e => e.IdAsignacion).HasColumnName("Id_Asignacion");
            entity.Property(e => e.FechaFin).HasColumnName("Fecha_Fin");
            entity.Property(e => e.FechaInicio).HasColumnName("Fecha_Inicio");
            entity.Property(e => e.IdPersona).HasColumnName("Id_Persona");
            entity.Property(e => e.IdSucursal).HasColumnName("Id_Sucursal");
            entity.Property(e => e.IdTurno).HasColumnName("Id_Turno");

            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.AsignacionTurno)
                .HasForeignKey(d => d.IdPersona)
                .HasConstraintName("FK_Asignacion_Turno_Persona");

            entity.HasOne(d => d.IdSucursalNavigation).WithMany(p => p.AsignacionTurno)
                .HasForeignKey(d => d.IdSucursal)
                .HasConstraintName("FK_Asignacion_Turno_Sucursal");

            entity.HasOne(d => d.IdTurnoNavigation).WithMany(p => p.AsignacionTurno)
                .HasForeignKey(d => d.IdTurno)
                .HasConstraintName("FK_Asignacion_Turno_Turno_Trabajo");
        });

        modelBuilder.Entity<Bitacora>(entity =>
        {
            entity.HasKey(e => e.IdBitacora);

            entity.Property(e => e.IdBitacora).HasColumnName("Id_Bitacora");
            entity.Property(e => e.Actividad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaHora).HasColumnType("datetime");
            entity.Property(e => e.Modulo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Responsable)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TipoAcceso).HasColumnName("Tipo_Acceso");

            entity.HasOne(d => d.TipoAccesoNavigation).WithMany(p => p.Bitacora)
                .HasForeignKey(d => d.TipoAcceso)
                .HasConstraintName("FK_Bitacora_TipoAcceso");
        });

        modelBuilder.Entity<Caja>(entity =>
        {
            entity.HasKey(e => e.IdCaja);

            entity.Property(e => e.IdCaja).HasColumnName("Id_Caja");
            entity.Property(e => e.IdSucursal).HasColumnName("Id_Sucursal");
            entity.Property(e => e.NombreCaja)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Nombre_Caja");

            entity.HasOne(d => d.IdSucursalNavigation).WithMany(p => p.Caja)
                .HasForeignKey(d => d.IdSucursal)
                .HasConstraintName("FK_Caja_Sucursal");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria);

            entity.Property(e => e.IdCategoria).HasColumnName("Id_Categoria");
            entity.Property(e => e.DescripcionCategoria)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("Descripcion_Categoria");
            entity.Property(e => e.NombreCategoria)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Categoria");
        });

        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(e => e.IdDepartamento);

            entity.Property(e => e.IdDepartamento).HasColumnName("Id_Departamento");
            entity.Property(e => e.NombreDepartamento)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Departamento");
        });

        modelBuilder.Entity<DetalleOrdenRes>(entity =>
        {
            entity.HasKey(e => e.IdDetalle);

            entity.Property(e => e.IdDetalle).HasColumnName("Id_Detalle");
            entity.Property(e => e.CantidadSolicitada).HasColumnName("Cantidad_Solicitada");
            entity.Property(e => e.IdProducto).HasColumnName("Id_Producto");
            entity.Property(e => e.PrecioUnitario).HasColumnName("Precio_Unitario");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleOrdenRes)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_DetalleOrdenRes_Producto");
        });

        modelBuilder.Entity<Direccion>(entity =>
        {
            entity.HasKey(e => e.IdDireccion);

            entity.Property(e => e.IdDireccion).HasColumnName("Id_Direccion");
            entity.Property(e => e.Direccion1)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("Direccion");
            entity.Property(e => e.IdMunicipio).HasColumnName("Id_Municipio");

            entity.HasOne(d => d.IdMunicipioNavigation).WithMany(p => p.Direccion)
                .HasForeignKey(d => d.IdMunicipio)
                .HasConstraintName("FK_Direccion_Municipio");
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.IdEstado);

            entity.Property(e => e.IdEstado).HasColumnName("Id_Estado");
            entity.Property(e => e.Estado1)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("Estado");
        });

        modelBuilder.Entity<EstadoCivil>(entity =>
        {
            entity.HasKey(e => e.IdEstadoCivil);

            entity.ToTable("Estado_Civil");

            entity.Property(e => e.IdEstadoCivil).HasColumnName("Id_EstadoCivil");
            entity.Property(e => e.EstadoCivil1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EstadoCivil");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.IdFactura);

            entity.Property(e => e.IdFactura).HasColumnName("Id_Factura");
            entity.Property(e => e.FechaVenta)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Venta");
            entity.Property(e => e.IdAperturaCaja).HasColumnName("Id_AperturaCaja");
            entity.Property(e => e.IdDetalleFactura).HasColumnName("Id_DetalleFactura");
            entity.Property(e => e.IdEstado).HasColumnName("Id_Estado");
            entity.Property(e => e.IdTipoPago).HasColumnName("Id_TipoPago");
            entity.Property(e => e.NumeroFactura).HasColumnName("Numero_Factura");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.IdAperturaCajaNavigation).WithMany(p => p.Factura)
                .HasForeignKey(d => d.IdAperturaCaja)
                .HasConstraintName("FK_Factura_AperturaCaja");

            entity.HasOne(d => d.IdDetalleFacturaNavigation).WithMany(p => p.Factura)
                .HasForeignKey(d => d.IdDetalleFactura)
                .HasConstraintName("FK_Factura_FacturaDetalle");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Factura)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_Factura_Estado");

            entity.HasOne(d => d.IdTipoPagoNavigation).WithMany(p => p.Factura)
                .HasForeignKey(d => d.IdTipoPago)
                .HasConstraintName("FK_Factura_TipoPago");
        });

        modelBuilder.Entity<FacturaDetalle>(entity =>
        {
            entity.HasKey(e => e.IdFacturaDetalle);

            entity.Property(e => e.IdFacturaDetalle).HasColumnName("Id_FacturaDetalle");
            entity.Property(e => e.IdProducto).HasColumnName("Id_Producto");
            entity.Property(e => e.PrecioUnitario).HasColumnName("Precio_Unitario");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.FacturaDetalle)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_FacturaDetalle_Producto");
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.HasKey(e => e.IdGenero);

            entity.Property(e => e.IdGenero).HasColumnName("Id_Genero");
            entity.Property(e => e.Ngenero)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NGenero");
        });

        modelBuilder.Entity<ImagenProducto>(entity =>
        {
            entity.HasKey(e => e.IdImagen);

            entity.Property(e => e.IdImagen).HasColumnName("Id_Imagen");
            entity.Property(e => e.IdProducto).HasColumnName("Id_Producto");
            entity.Property(e => e.Imagen).IsUnicode(false);
        });

        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.HasKey(e => e.IdInventario);

            entity.Property(e => e.IdInventario).HasColumnName("Id_Inventario");
            entity.Property(e => e.NombreInventario)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Inventario");
            entity.Property(e => e.StockMaximo).HasColumnName("Stock_Maximo");
            entity.Property(e => e.StockMinimo).HasColumnName("Stock_Minimo");
            entity.Property(e => e.UltimaActualizacion)
                .HasColumnType("datetime")
                .HasColumnName("Ultima_Actualizacion");
        });

        modelBuilder.Entity<Municipio>(entity =>
        {
            entity.HasKey(e => e.IdMunicipio);

            entity.Property(e => e.IdMunicipio).HasColumnName("Id_Municipio");
            entity.Property(e => e.IdDepartamento).HasColumnName("Id_Departamento");
            entity.Property(e => e.NombreMunicipio)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Municipio");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Municipio)
                .HasForeignKey(d => d.IdDepartamento)
                .HasConstraintName("FK_Municipio_Departamento");
        });

        modelBuilder.Entity<OrdenRestablecimiento>(entity =>
        {
            entity.HasKey(e => e.IdOrden);

            entity.Property(e => e.IdOrden).HasColumnName("Id_Orden");
            entity.Property(e => e.FechaOrden)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Orden");
            entity.Property(e => e.FechaRecepcion)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Recepcion");
            entity.Property(e => e.IdEstado).HasColumnName("Id_Estado");
            entity.Property(e => e.IdPersonaSolicitud).HasColumnName("id_PersonaSolicitud");
            entity.Property(e => e.IdProveedor).HasColumnName("Id_Proveedor");
            entity.Property(e => e.IdSucursal).HasColumnName("Id_Sucursal");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.IdDetalleOrdenNavigation).WithMany(p => p.OrdenRestablecimiento)
                .HasForeignKey(d => d.IdDetalleOrden)
                .HasConstraintName("FK_OrdenRestablecimiento_DetalleOrdenRes");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.OrdenRestablecimiento)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_OrdenRestablecimiento_Estado");

            entity.HasOne(d => d.IdPersonaSolicitudNavigation).WithMany(p => p.OrdenRestablecimientoIdPersonaSolicitudNavigation)
                .HasForeignKey(d => d.IdPersonaSolicitud)
                .HasConstraintName("FK_OrdenRestablecimiento_Persona");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.OrdenRestablecimiento)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("FK_OrdenRestablecimiento_Proveedor");

            entity.HasOne(d => d.IdSucursalNavigation).WithMany(p => p.OrdenRestablecimiento)
                .HasForeignKey(d => d.IdSucursal)
                .HasConstraintName("FK_OrdenRestablecimiento_Sucursal");

            entity.HasOne(d => d.UsuarioAprobacionNavigation).WithMany(p => p.OrdenRestablecimientoUsuarioAprobacionNavigation)
                .HasForeignKey(d => d.UsuarioAprobacion)
                .HasConstraintName("FK_OrdenRestablecimiento_Persona1");
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.HasKey(e => e.IdPersona);

            entity.Property(e => e.IdPersona).HasColumnName("Id_Persona");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Contraseña)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Dpi).HasColumnName("DPI");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Creacion");
            entity.Property(e => e.FechaModificacion)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Modificacion");
            entity.Property(e => e.FechaNacimiento).HasColumnName("Fecha_Nacimiento");
            entity.Property(e => e.FechaRegistro)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("Fecha_Registro");
            entity.Property(e => e.IdDireccion).HasColumnName("Id_Direccion");
            entity.Property(e => e.IdEstadoCivil).HasColumnName("Id_EstadoCivil");
            entity.Property(e => e.IdGenero).HasColumnName("Id_Genero");
            entity.Property(e => e.IdRool).HasColumnName("Id_Rool");
            entity.Property(e => e.IdSucursal).HasColumnName("Id_Sucursal");
            entity.Property(e => e.IdTelefono).HasColumnName("Id_Telefono");
            entity.Property(e => e.Nit).HasColumnName("NIT");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioCreacion)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Usuario_Creacion");
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Usuario_Modificacion");

            entity.HasOne(d => d.IdDireccionNavigation).WithMany(p => p.Persona)
                .HasForeignKey(d => d.IdDireccion)
                .HasConstraintName("FK_Persona_Direccion");

            entity.HasOne(d => d.IdEstadoCivilNavigation).WithMany(p => p.Persona)
                .HasForeignKey(d => d.IdEstadoCivil)
                .HasConstraintName("FK_Persona_Estado_Civil");

            entity.HasOne(d => d.IdGeneroNavigation).WithMany(p => p.Persona)
                .HasForeignKey(d => d.IdGenero)
                .HasConstraintName("FK_Persona_Genero");

            entity.HasOne(d => d.IdRoolNavigation).WithMany(p => p.Persona)
                .HasForeignKey(d => d.IdRool)
                .HasConstraintName("FK_Persona_Rol");

            entity.HasOne(d => d.IdTelefonoNavigation).WithMany(p => p.Persona)
                .HasForeignKey(d => d.IdTelefono)
                .HasConstraintName("FK_Persona_Telefono");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto);

            entity.Property(e => e.IdProducto).HasColumnName("Id_Producto");
            entity.Property(e => e.CantidadMaxima).HasColumnName("Cantidad_Maxima");
            entity.Property(e => e.DescrpcionProducto)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Descrpcion_Producto");
            entity.Property(e => e.FechaCompra).HasColumnName("Fecha_Compra");
            entity.Property(e => e.FechaVencimiento).HasColumnName("Fecha_Vencimiento");
            entity.Property(e => e.IdCategoria).HasColumnName("Id_Categoria");
            entity.Property(e => e.IdImagen).HasColumnName("Id_Imagen");
            entity.Property(e => e.IdInventario).HasColumnName("Id_Inventario");
            entity.Property(e => e.IdProveedor).HasColumnName("Id_Proveedor");
            entity.Property(e => e.MedicamentoControlado).HasColumnName("Medicamento_Controlado");
            entity.Property(e => e.NivelReorden).HasColumnName("Nivel_Reorden");
            entity.Property(e => e.NombreProducto)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Producto");
            entity.Property(e => e.PrecioCompra).HasColumnName("Precio_Compra");
            entity.Property(e => e.PrecioVenta).HasColumnName("Precio_Venta");
            entity.Property(e => e.RequiereReceta).HasColumnName("Requiere_Receta");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Unidad_Medida");

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Producto)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Producto_Categoria");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.Producto)
                .HasForeignKey(d => d.IdImagen)
                .HasConstraintName("FK_Producto_ImagenProducto");

            entity.HasOne(d => d.IdInventarioNavigation).WithMany(p => p.Producto)
                .HasForeignKey(d => d.IdInventario)
                .HasConstraintName("FK_Producto_Inventario");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Producto)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("FK_Producto_Proveedor");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor);

            entity.Property(e => e.IdProveedor).HasColumnName("Id_Proveedor");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdDireccion).HasColumnName("Id_Direccion");
            entity.Property(e => e.IdTelefono).HasColumnName("Id_Telefono");
            entity.Property(e => e.NombreProveedor)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Proveedor");
            entity.Property(e => e.PersonaContacto).HasColumnName("Persona_Contacto");

            entity.HasOne(d => d.IdDireccionNavigation).WithMany(p => p.Proveedor)
                .HasForeignKey(d => d.IdDireccion)
                .HasConstraintName("FK_Proveedor_Direccion");

            entity.HasOne(d => d.IdTelefonoNavigation).WithMany(p => p.Proveedor)
                .HasForeignKey(d => d.IdTelefono)
                .HasConstraintName("FK_Proveedor_Telefono");

            entity.HasOne(d => d.PersonaContactoNavigation).WithMany(p => p.Proveedor)
                .HasForeignKey(d => d.PersonaContacto)
                .HasConstraintName("FK_Proveedor_Persona");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.Property(e => e.IdRol).HasColumnName("Id_Rol");
            entity.Property(e => e.DescripcionRol)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Descripcion_Rol");
            entity.Property(e => e.TipoRol)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Tipo_Rol");
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.HasKey(e => e.IdSucursal);

            entity.Property(e => e.IdSucursal).HasColumnName("Id_Sucursal");
            entity.Property(e => e.EmailSucursal)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Email_Sucursal");
            entity.Property(e => e.HorarioApertura).HasColumnName("Horario_Apertura");
            entity.Property(e => e.HorarioCierre).HasColumnName("Horario_Cierre");
            entity.Property(e => e.IdDireccion).HasColumnName("Id_Direccion");
            entity.Property(e => e.IdInventario).HasColumnName("Id_Inventario");
            entity.Property(e => e.IdTelefono).HasColumnName("Id_Telefono");
            entity.Property(e => e.NombreSucursal)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Nombre_Sucursal");
            entity.Property(e => e.ResponsableSucursal).HasColumnName("Responsable_Sucursal");

            entity.HasOne(d => d.IdDireccionNavigation).WithMany(p => p.Sucursal)
                .HasForeignKey(d => d.IdDireccion)
                .HasConstraintName("FK_Sucursal_Direccion");

            entity.HasOne(d => d.IdInventarioNavigation).WithMany(p => p.Sucursal)
                .HasForeignKey(d => d.IdInventario)
                .HasConstraintName("FK_Sucursal_Inventario");

            entity.HasOne(d => d.IdTelefonoNavigation).WithMany(p => p.Sucursal)
                .HasForeignKey(d => d.IdTelefono)
                .HasConstraintName("FK_Sucursal_Telefono");

            entity.HasOne(d => d.ResponsableSucursalNavigation).WithMany(p => p.Sucursal)
                .HasForeignKey(d => d.ResponsableSucursal)
                .HasConstraintName("FK_Sucursal_Persona");
        });

        modelBuilder.Entity<Telefono>(entity =>
        {
            entity.HasKey(e => e.IdTelefono);

            entity.Property(e => e.IdTelefono).HasColumnName("Id_Telefono");
            entity.Property(e => e.NumeroTelefonico).HasColumnName("Numero_Telefonico");
        });

        modelBuilder.Entity<TipoAcceso>(entity =>
        {
            entity.HasKey(e => e.IdTipoAcceso);

            entity.Property(e => e.IdTipoAcceso).HasColumnName("Id_TipoAcceso");
            entity.Property(e => e.NombreAcceso)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TipoPago>(entity =>
        {
            entity.HasKey(e => e.IdTipoPago);

            entity.Property(e => e.IdTipoPago).HasColumnName("Id_TipoPago");
            entity.Property(e => e.NombrePago)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Nombre_Pago");
        });

        modelBuilder.Entity<Traslado>(entity =>
        {
            entity.HasKey(e => e.IdTraslado);

            entity.Property(e => e.IdTraslado).HasColumnName("Id_Traslado");
            entity.Property(e => e.FechaTraslado)
                .HasColumnType("datetime")
                .HasColumnName("Fecha_Traslado");
            entity.Property(e => e.IdEstadoTraslado).HasColumnName("Id_EstadoTraslado");
            entity.Property(e => e.IdSucursalDestino).HasColumnName("Id_SucursalDestino");
            entity.Property(e => e.IdSucursalOrigen).HasColumnName("Id_SucursalOrigen");
            entity.Property(e => e.IdTrasladodetalles).HasColumnName("Id_trasladodetalles");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.IdEstadoTrasladoNavigation).WithMany(p => p.Traslado)
                .HasForeignKey(d => d.IdEstadoTraslado)
                .HasConstraintName("FK_Traslado_Estado");

            entity.HasOne(d => d.IdSucursalDestinoNavigation).WithMany(p => p.TrasladoIdSucursalDestinoNavigation)
                .HasForeignKey(d => d.IdSucursalDestino)
                .HasConstraintName("FK_Traslado_Sucursal1");

            entity.HasOne(d => d.IdSucursalOrigenNavigation).WithMany(p => p.TrasladoIdSucursalOrigenNavigation)
                .HasForeignKey(d => d.IdSucursalOrigen)
                .HasConstraintName("FK_Traslado_Sucursal");

            entity.HasOne(d => d.IdTrasladodetallesNavigation).WithMany(p => p.Traslado)
                .HasForeignKey(d => d.IdTrasladodetalles)
                .HasConstraintName("FK_Traslado_TrasladoDetalle");
        });

        modelBuilder.Entity<TrasladoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdTrasladoDetalle);

            entity.Property(e => e.IdTrasladoDetalle).HasColumnName("Id_TrasladoDetalle");
            entity.Property(e => e.IdEstado).HasColumnName("Id_Estado");
            entity.Property(e => e.IdProducto).HasColumnName("Id_Producto");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.TrasladoDetalle)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_TrasladoDetalle_Estado");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.TrasladoDetalle)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_TrasladoDetalle_Producto");
        });

        modelBuilder.Entity<TurnoTrabajo>(entity =>
        {
            entity.HasKey(e => e.IdTurno);

            entity.ToTable("Turno_Trabajo");

            entity.Property(e => e.IdTurno).HasColumnName("Id_Turno");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HoraFin)
                .HasColumnType("datetime")
                .HasColumnName("Hora_Fin");
            entity.Property(e => e.HoraInicio)
                .HasColumnType("datetime")
                .HasColumnName("Hora_Inicio");
            entity.Property(e => e.NombreTurno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Nombre_Turno");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
