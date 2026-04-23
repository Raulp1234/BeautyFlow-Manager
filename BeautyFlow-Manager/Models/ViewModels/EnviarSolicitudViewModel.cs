using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Models
{
    public class EnviarSolicitudViewModel
    {
        [Required]
        public Guid SalonId { get; set; }
        
        [Display(Name = "Salón")]
        public string? NombreSalon { get; set; }
        
        [Display(Name = "Dirección")]
        public string? DireccionSalon { get; set; }
        
        [Display(Name = "Teléfono")]
        public string? TelefonoSalon { get; set; }
        
        [Display(Name = "Dueño del Salón")]
        public string? DueñoSalon { get; set; }
        
        [Display(Name = "Mensaje para el dueño")]
        [StringLength(500, ErrorMessage = "El mensaje no puede exceder los 500 caracteres")]
        public string? MensajeTrabajador { get; set; }
    }
}
