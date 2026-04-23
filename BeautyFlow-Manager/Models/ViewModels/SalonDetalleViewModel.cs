using BeautyFlow_Manager.Models;

namespace BeautyFlow_Manager.Models.ViewModels
{
    public class SalonDetalleViewModel
    {
        public Salon Salon { get; set; } = null!;
        public IEnumerable<Servicio> Servicios { get; set; } = new List<Servicio>();
        public IEnumerable<SalonSuscripcion> Suscripciones { get; set; } = new List<SalonSuscripcion>();
        public IEnumerable<Reserva>? Reservas { get; set; }
    }
}
