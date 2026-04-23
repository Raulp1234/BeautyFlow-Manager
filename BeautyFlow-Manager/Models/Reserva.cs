using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyFlow_Manager.Models
{
    public class Reserva
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Relación con el cliente que reserva
        [Required]
        [Display(Name = "Cliente")]
        public Guid ClienteId { get; set; }
        public Usuario? Cliente { get; set; }
        
        // Relación con el servicio reservado
        [Required]
        [Display(Name = "Servicio")]
        public Guid ServicioId { get; set; }
        public Servicio? Servicio { get; set; }
        
        // Relación con el trabajador que brindará el servicio
        [Required]
        [Display(Name = "Trabajador")]
        public Guid TrabajadorId { get; set; }
        public TrabajadorIndependiente? Trabajador { get; set; }
        
        // Relación con el salón donde se brinda el servicio
        [Required]
        [Display(Name = "Salón")]
        public Guid SalonId { get; set; }
        public Salon? Salon { get; set; }
        
        // Fecha y hora de inicio de la cita
        [Required]
        [Display(Name = "Fecha y Hora de Inicio")]
        public DateTime FechaHoraInicio { get; set; }
        
        // Fecha y hora de fin (calculada según duración del servicio)
        [Display(Name = "Fecha y Hora de Fin")]
        public DateTime FechaHoraFin { get; set; }
        
        // Estado de la reserva
        [Required]
        [Display(Name = "Estado")]
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
        
        // Costo total al momento de la reserva
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo Total")]
        public decimal CostoTotal { get; set; }
        
        // Notas del cliente
        [StringLength(500)]
        [Display(Name = "Notas del Cliente")]
        public string? NotasCliente { get; set; }
        
        // Notas internas del salón/trabajador
        [StringLength(500)]
        [Display(Name = "Notas Internas")]
        public string? NotasInternas { get; set; }
        
        // Motivo de cancelación (si aplica)
        [StringLength(500)]
        [Display(Name = "Motivo de Cancelación")]
        public string? MotivoCancelacion { get; set; }
        
        // Fechas de auditoría
        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Fecha de Confirmación")]
        public DateTime? FechaConfirmacion { get; set; }
        
        [Display(Name = "Fecha de Cancelación")]
        public DateTime? FechaCancelacion { get; set; }
        
        [Display(Name = "Fecha de Última Actualización")]
        public DateTime? FechaUltimaActualizacion { get; set; }
        
        // Método de pago
        [Display(Name = "Método de Pago")]
        [StringLength(50)]
        public string? MetodoPago { get; set; }
        
        // Estado del pago
        [Display(Name = "Estado del Pago")]
        public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;
        
        // Referencia de pago
        [Display(Name = "Referencia de Pago")]
        [StringLength(100)]
        public string? ReferenciaPago { get; set; }
    }
    
    public enum EstadoReserva
    {
        Pendiente = 0,        // Esperando confirmación del salón/trabajador
        Confirmada = 1,       // Confirmada por el salón/trabajador
        EnProceso = 2,        // El servicio se está brindando
        Completada = 3,       // Servicio completado exitosamente
        Cancelada = 4,        // Cancelada por cliente o salón
        NoShow = 5,           // Cliente no se presentó
        Reagendada = 6        // Fue reagendada a otra fecha
    }
    
    public enum EstadoPago
    {
        Pendiente = 0,
        Pagado = 1,
        Reembolsado = 2,
        Parcial = 3,
        Cancelado = 4
    }
}
