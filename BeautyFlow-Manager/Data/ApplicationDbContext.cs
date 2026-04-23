using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Models;
using Microsoft.AspNetCore.Identity;

namespace BeautyFlow_Manager.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Salon> Salones { get; set; }
        public DbSet<TrabajadorIndependiente> TrabajadoresIndependientes { get; set; }
        public DbSet<SolicitudContrato> SolicitudesContrato { get; set; }
        
        // Nuevas entidades para el sistema de reservas y suscripciones
        public DbSet<TipoSuscripcion> TiposSuscripcion { get; set; }
        public DbSet<SalonSuscripcion> SalonesSuscripciones { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<ServicioTrabajador> ServiciosTrabajadores { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar tabla Rol personalizada
            builder.Entity<Rol>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Nombre).IsUnique();
            });

            // Configurar Usuario con RolId como foreign key
            builder.Entity<Usuario>(entity =>
            {
                entity.Property(e => e.NombreCompleto).IsRequired();
                entity.Property(e => e.Activo).HasDefaultValue(true);
                entity.Property(e => e.FechaRegistro).HasDefaultValue(DateTime.UtcNow);
            });

            // Configurar Salon
            builder.Entity<Salon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreSalon).IsRequired();
                entity.HasIndex(e => e.NombreSalon).IsUnique();
                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar TrabajadorIndependiente
            builder.Entity<TrabajadorIndependiente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreCompleto).IsRequired();
                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                // Relación con contrato actual
                entity.HasOne(e => e.ContratoActual)
                      .WithMany()
                      .HasForeignKey(e => e.ContratoActualId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
            
            // Configurar SolicitudContrato
            builder.Entity<SolicitudContrato>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Trabajador)
                      .WithMany()
                      .HasForeignKey(e => e.TrabajadorId)
                      .OnDelete(DeleteBehavior.Restrict); // Cambiado a Restrict para evitar cascade paths
                      
                entity.HasOne(e => e.Salon)
                      .WithMany(s => s.SolicitudesRecibidas)
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.Property(e => e.Estado).HasDefaultValue(EstadoSolicitud.Pendiente);
                
                // Un trabajador solo puede tener una solicitud pendiente a la vez
                entity.HasIndex(e => new { e.TrabajadorId, e.Estado })
                      .HasFilter("Estado = 0"); // Solo para pendientes
            });
            
            // Configurar TipoSuscripcion
            builder.Entity<TipoSuscripcion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Nombre).IsUnique();
                entity.Property(e => e.Activo).HasDefaultValue(true);
            });
            
            // Configurar SalonSuscripcion
            builder.Entity<SalonSuscripcion>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Salon)
                      .WithMany()
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.TipoSuscripcion)
                      .WithMany(t => t.SalonesSuscritos)
                      .HasForeignKey(e => e.TipoSuscripcionId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasIndex(e => e.SalonId);
                entity.HasIndex(e => e.Estado);
            });
            
            // Configurar Servicio
            builder.Entity<Servicio>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Salon)
                      .WithMany()
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Activo).HasDefaultValue(true);
                
                entity.HasIndex(e => e.SalonId);
                entity.HasIndex(e => e.Activo);
            });
            
            // Configurar ServicioTrabajador (tabla intermedia)
            builder.Entity<ServicioTrabajador>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Servicio)
                      .WithMany(s => s.ServiciosTrabajadores)
                      .HasForeignKey(e => e.ServicioId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Trabajador)
                      .WithMany()
                      .HasForeignKey(e => e.TrabajadorId)
                      .OnDelete(DeleteBehavior.Restrict); // Cambiado a Restrict para evitar cascade paths
                      
                entity.Property(e => e.Activo).HasDefaultValue(true);
                
                // Un trabajador no debe estar duplicado en el mismo servicio
                entity.HasIndex(e => new { e.ServicioId, e.TrabajadorId }).IsUnique();
            });
            
            // Configurar Reserva
            builder.Entity<Reserva>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Cliente)
                      .WithMany()
                      .HasForeignKey(e => e.ClienteId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Servicio)
                      .WithMany(s => s.Reservas)
                      .HasForeignKey(e => e.ServicioId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(e => e.Trabajador)
                      .WithMany()
                      .HasForeignKey(e => e.TrabajadorId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(e => e.Salon)
                      .WithMany()
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.Property(e => e.Estado).HasDefaultValue(EstadoReserva.Pendiente);
                entity.Property(e => e.EstadoPago).HasDefaultValue(EstadoPago.Pendiente);
                
                entity.HasIndex(e => e.FechaHoraInicio);
                entity.HasIndex(e => e.TrabajadorId);
                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.Estado);
            });
        }
    }
}
