using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyFlow_Manager.Models
{
    public class TipoSuscripcion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Nombre del Plan")]
        public string Nombre { get; set; } = string.Empty; // Ej: FREE, BASIC, PREMIUM, ENTERPRISE
        
        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio Mensual")]
        public decimal PrecioMensual { get; set; } = 0.00m;
        
        [Display(Name = "Precio Anual")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioAnual { get; set; } // Opcional, con descuento
        
        [Display(Name = "Duración del Contrato (días)")]
        public int DuracionDias { get; set; } = 30; // Por defecto 30 días
        
        // Límites y características
        [Display(Name = "Máximo de Trabajadores")]
        public int? MaxTrabajadores { get; set; } // null = ilimitado
        
        [Display(Name = "Máximo de Servicios")]
        public int? MaxServicios { get; set; } // null = ilimitado
        
        [Display(Name = "Comisión por Reserva (%)")]
        [Range(0, 100)]
        public decimal ComisionPorcentaje { get; set; } = 0.00m;
        
        [Display(Name = "Permite Múltiples Ubicaciones")]
        public bool PermiteMultiplesUbicaciones { get; set; } = false;
        
        [Display(Name = "Acceso a Reportes Avanzados")]
        public bool IncluyeReportesAvanzados { get; set; } = false;
        
        [Display(Name = "Soporte Prioritario")]
        public bool IncluyeSoportePrioritario { get; set; } = false;
        
        [Display(Name = "Personalización de Marca")]
        public bool IncluyePersonalizacionMarca { get; set; } = false;
        
        // Estado del plan
        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
        
        [Display(Name = "Es el Plan por Defecto")]
        public bool EsPlanDefecto { get; set; } = false;
        
        // Relaciones
        [Display(Name = "Salones Suscritos")]
        public ICollection<SalonSuscripcion>? SalonesSuscritos { get; set; }
    }
}
