using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyFlow_Manager.Models
{
    // Tabla intermedia para la relación muchos-a-muchos entre Servicio y Trabajador
    public class ServicioTrabajador
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [Display(Name = "Servicio")]
        public Guid ServicioId { get; set; }
        public Servicio? Servicio { get; set; }
        
        [Required]
        [Display(Name = "Trabajador")]
        public Guid TrabajadorId { get; set; }
        public TrabajadorIndependiente? Trabajador { get; set; }
        
        // El trabajador puede tener un costo diferente para este servicio en este salón
        [Display(Name = "Costo Personalizado")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostoPersonalizado { get; set; } // Si es null, usa el costo base del servicio
        
        // Estado de esta asignación
        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
        
        [Display(Name = "Fecha de Asignación")]
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Notas Internas")]
        [StringLength(300)]
        public string? NotasInternas { get; set; }
        
        // Propiedades de navegación para acceder a datos del trabajador relacionado
        [NotMapped]
        [Display(Name = "Nombre")]
        public string? Nombre => Trabajador?.NombreCompleto;
        
        [NotMapped]
        [Display(Name = "Foto")]
        public string? FotoUrl => Trabajador?.FotoUrl;
        
        [NotMapped]
        [Display(Name = "Especialidad")]
        public string? Especialidad => Trabajador?.EspecialidadPrincipal;
        
        [NotMapped]
        [Display(Name = "Años de Experiencia")]
        public int? AniosExperiencia => Trabajador?.AniosExperiencia;
    }
}
