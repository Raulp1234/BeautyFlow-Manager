using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyFlow_Manager.Models
{
    public class SalonSuscripcion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Relación con el salón
        [Required]
        [Display(Name = "Salón")]
        public Guid SalonId { get; set; }
        public Salon? Salon { get; set; }
        
        // Relación con el tipo de suscripción
        [Required]
        [Display(Name = "Plan de Suscripción")]
        public Guid TipoSuscripcionId { get; set; }
        public TipoSuscripcion? TipoSuscripcion { get; set; }
        
        // Fechas del ciclo actual
        [Required]
        [Display(Name = "Fecha de Inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Fecha de Fin")]
        public DateTime? FechaFin { get; set; }
        
        [Display(Name = "Fecha de Próximo Pago")]
        public DateTime? FechaProximoPago { get; set; }
        
        // Estado de la suscripción
        [Required]
        [Display(Name = "Estado")]
        public EstadoSuscripcion Estado { get; set; } = EstadoSuscripcion.Activa;
        
        // Método de pago y referencia
        [Display(Name = "Método de Pago")]
        [StringLength(50)]
        public string? MetodoPago { get; set; } // Tarjeta, Transferencia, Efectivo, etc.
        
        [Display(Name = "Referencia de Pago")]
        [StringLength(100)]
        public string? ReferenciaPago { get; set; }
        
        // Historial y notas
        [Display(Name = "Fecha de Última Actualización")]
        public DateTime FechaUltimaActualizacion { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Notas Administrativas")]
        [StringLength(500)]
        public string? NotasAdministrativas { get; set; }
        
        // Auditoría
        [Display(Name = "Usuario que Registró")]
        public Guid? UsuarioRegistroId { get; set; }
        public Usuario? UsuarioRegistro { get; set; }
    }
    
    public enum EstadoSuscripcion
    {
        Activa = 0,
        PendientePago = 1,
        Vencida = 2,
        Cancelada = 3,
        Suspendida = 4,
        Prueba = 5
    }
}
