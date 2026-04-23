using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyFlow_Manager.Models
{
    public class Servicio
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Relación con el salón (el dueño crea el servicio)
        [Required]
        [Display(Name = "Salón")]
        public Guid SalonId { get; set; }
        public Salon? Salon { get; set; }
        
        // Información del servicio
        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre del Servicio")]
        public string Nombre { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }
        
        // Duración en minutos
        [Required]
        [Display(Name = "Duración (minutos)")]
        [Range(5, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos")]
        public int DuracionMinutos { get; set; }
        
        // Costo del servicio
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo")]
        [Range(0, 99999.99)]
        public decimal Costo { get; set; }
        
        // Categoría del servicio (opcional)
        [StringLength(50)]
        [Display(Name = "Categoría")]
        public string? Categoria { get; set; } // Ej: Corte, Color, Manicure, etc.
        
        // Estado del servicio
        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
        
        [Display(Name = "Requiere Reserva Anticipada (días)")]
        public int? DiasAntelacionRequerida { get; set; } = 0;
        
        // Imagen del servicio (opcional)
        [Display(Name = "Imagen URL")]
        [StringLength(500)]
        public string? ImagenUrl { get; set; }
        
        // Fechas de auditoría
        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Fecha de Última Actualización")]
        public DateTime? FechaUltimaActualizacion { get; set; }
        
        // Relaciones - Trabajadores que pueden ofrecer este servicio
        [Display(Name = "Trabajadores Asignados")]
        public ICollection<ServicioTrabajador>? ServiciosTrabajadores { get; set; }
        
        // Relaciones - Citas/reservas de este servicio
        [Display(Name = "Reservas")]
        public ICollection<Reserva>? Reservas { get; set; }
    }
}
