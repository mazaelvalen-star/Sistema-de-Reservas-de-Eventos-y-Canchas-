using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtepReservas.Api.Data;

/// <summary>
/// Puente entre las clases de C# y las tablas de MySQL. Las tablas las crea
/// database/01_esquema.sql (DER v4): acá solo se le explica a EF Core cómo se corresponden.
/// </summary>
public class AtepDbContext : DbContext
{
    public AtepDbContext(DbContextOptions<AtepDbContext> options) : base(options) { }

    // Un DbSet por tabla: es lo que se consulta con LINQ (_db.Espacios.Where(...)).
    public DbSet<Domicilio> Domicilios => Set<Domicilio>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<Espacio> Espacios => Set<Espacio>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<TipoPago> TiposPago => Set<TipoPago>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<ReservaEspacio> ReservasEspacios => Set<ReservaEspacio>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<DetallePago> DetallesPago => Set<DetallePago>();

    // Reglas que valen para todas las propiedades de un mismo tipo.
    protected override void ConfigureConventions(ModelConfigurationBuilder configuracion)
    {
        configuracion.Properties<decimal>().HavePrecision(12, 2);              // DECIMAL(12,2)
        configuracion.Properties<TipoEspacio>().HaveConversion<string>();       // enum <-> texto
        configuracion.Properties<CondicionTarifaria>().HaveConversion<string>();
        configuracion.Properties<EstadoReserva>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // Claves primarias y relaciones, como en el DER. Hay que declararlas porque EF solo
        // las adivina si se llaman "Id" o "EspacioId"; nosotros usamos "IdEspacio".
        modelo.Entity<Domicilio>().HasKey(d => d.IdDomicilio);

        modelo.Entity<Perfil>().HasKey(p => p.IdPerfil);

        modelo.Entity<Permiso>(e =>
        {
            e.HasKey(p => p.IdPermiso);
            e.HasOne(p => p.PerfilMinimo).WithMany(p => p.Permisos).HasForeignKey(p => p.IdPerfilMinimo);
        });

        modelo.Entity<Usuario>(e =>
        {
            e.HasKey(u => u.IdUsuario);
            e.HasOne(u => u.Perfil).WithMany(p => p.Usuarios).HasForeignKey(u => u.IdPerfil);
        });

        modelo.Entity<Cliente>(e =>
        {
            e.HasKey(c => c.IdCliente);
            // Uno a uno: cada cliente tiene su domicilio (S6) y puede tener un usuario.
            e.HasOne(c => c.Domicilio).WithOne().HasForeignKey<Cliente>(c => c.IdDomicilio);
            e.HasOne(c => c.Usuario).WithOne(u => u.Cliente).HasForeignKey<Cliente>(c => c.IdUsuario);
        });

        modelo.Entity<Empleado>(e =>
        {
            e.HasKey(x => x.IdEmpleado);
            e.HasOne(x => x.Domicilio).WithOne().HasForeignKey<Empleado>(x => x.IdDomicilio);
            e.HasOne(x => x.Usuario).WithOne(u => u.Empleado).HasForeignKey<Empleado>(x => x.IdUsuario);
        });

        modelo.Entity<Espacio>().HasKey(e => e.IdEspacio);

        modelo.Entity<Tarifa>(e =>
        {
            e.HasKey(t => t.IdTarifa);
            e.HasOne(t => t.Espacio).WithMany(x => x.Tarifas).HasForeignKey(t => t.IdEspacio);
        });

        modelo.Entity<TipoPago>().HasKey(t => t.IdTipoPago);

        modelo.Entity<Reserva>(e =>
        {
            e.HasKey(r => r.IdReserva);
            e.HasOne(r => r.Cliente).WithMany(c => c.Reservas).HasForeignKey(r => r.IdCliente);
            e.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.IdUsuario);
        });

        modelo.Entity<ReservaEspacio>(e =>
        {
            e.HasKey(re => re.IdReservaEspacio);
            e.HasOne(re => re.Reserva).WithMany(r => r.Espacios).HasForeignKey(re => re.IdReserva);
            e.HasOne(re => re.Espacio).WithMany(x => x.Reservas).HasForeignKey(re => re.IdEspacio);
        });

        modelo.Entity<Pago>(e =>
        {
            e.HasKey(p => p.IdPago);
            e.HasOne(p => p.Reserva).WithMany(r => r.Pagos).HasForeignKey(p => p.IdReserva);
        });

        modelo.Entity<DetallePago>(e =>
        {
            e.HasKey(d => d.IdDetallePago);
            e.HasOne(d => d.Pago).WithMany(p => p.Detalles).HasForeignKey(d => d.IdPago);
            e.HasOne(d => d.TipoPago).WithMany().HasForeignKey(d => d.IdTipoPago);
        });

        // Nombres: en C# usamos PascalCase y en MySQL snake_case, como en el DER.
        // En vez de escribir el nombre de cada tabla y columna, se traducen solos:
        //   DetallePago -> detalle_pago      FechaHoraInicio -> fecha_hora_inicio
        // Por eso, si agregan una propiedad, la columna tiene que llamarse igual en snake_case.
        foreach (var entidad in modelo.Model.GetEntityTypes())
        {
            entidad.SetTableName(ASnakeCase(entidad.ClrType.Name));
            foreach (var propiedad in entidad.GetProperties())
                propiedad.SetColumnName(ASnakeCase(propiedad.Name));
        }

        // La única excepción a la regla (va después del foreach para pisar el nombre automático).
        modelo.Entity<Usuario>().Property(u => u.NombreUsuario).HasColumnName("usuario");
    }

    private static string ASnakeCase(string nombre)
    {
        var resultado = new System.Text.StringBuilder();
        for (var i = 0; i < nombre.Length; i++)
        {
            if (i > 0 && char.IsUpper(nombre[i]))
                resultado.Append('_');
            resultado.Append(char.ToLowerInvariant(nombre[i]));
        }
        return resultado.ToString();
    }
}
