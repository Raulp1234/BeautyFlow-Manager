using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyFlow_Manager.Models
{
    public class SolicitudContrato
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [Display(Name = "Trabajador")]
        public Guid TrabajadorId { get; set; }
        public TrabajadorIndependiente? Trabajador { get; set; }
        
        [Required]
        [Display(Name = "Salón")]
        public Guid SalonId { get; set; }
        public Salon? Salon { get; set; }
        
        [Required]
        [Display(Name = "Estado")]
        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
        
        [Display(Name = "Fecha de Solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Fecha de Respuesta")]
        public DateTime? FechaRespuesta { get; set; }
        
        [Display(Name = "Mensaje del Trabajador")]
        [StringLength(500, ErrorMessage = "El mensaje no puede exceder los 500 caracteres")]
        public string? MensajeTrabajador { get; set; }
        
        [Display(Name = "Motivo de Rechazo")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder los 500 caracteres")]
        public string? MotivoRechazo { get; set; }
        
        [Display(Name = "Notas Adicionales")]
        [StringLength(1000)]
        public string? NotasAdicionales { get; set; }
    }
    
    public enum EstadoSolicitud
    {
        Pendiente = 0,
        Aceptada = 1,
        Rechazada = 2,
        Cancelada = 3,
        Finalizada = 4
    }
}
