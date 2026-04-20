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
            });
        }
    }
}
