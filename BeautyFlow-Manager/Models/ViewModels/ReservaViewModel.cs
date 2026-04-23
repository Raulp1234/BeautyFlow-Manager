using BeautyFlow_Manager.Models;

namespace BeautyFlow_Manager.Models.ViewModels
{
    public class ReservaViewModel
    {
        // Para búsqueda de salones
        public IEnumerable<Salon>? Salones { get; set; }

        // Para detalle de reserva y creación
        public Salon? Salon { get; set; }
        public Servicio? Servicio { get; set; }
        public Usuario? Trabajador { get; set; }

        // IDs para crear reserva
        public Guid ServicioId { get; set; }
        public Guid SalonId { get; set; }
        public Guid TrabajadorId { get; set; }

        // Trabajadores disponibles para un servicio
        public IEnumerable<ServicioTrabajador>? TrabajadoresDisponibles { get; set; }

        // Reservas del cliente
        public IEnumerable<Reserva>? ReservasProximas { get; set; }
        public IEnumerable<Reserva>? ReservasHistorial { get; set; }

        // Para mostrar detalles de reserva en vistas
        public Reserva? Reserva { get; set; }

        // Propiedades auxiliares para mostrar información en listas
        public string? ServicioNombre { get; set; }
        public string? SalonNombre { get; set; }
        public string? TrabajadorNombre { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public decimal CostoTotal { get; set; }
        public EstadoReserva Estado { get; set; }
        public EstadoPago EstadoPago { get; set; }
    }
}
