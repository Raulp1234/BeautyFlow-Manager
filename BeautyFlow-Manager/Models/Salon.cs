using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Models
{
    public class Salon
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required(ErrorMessage = "El nombre del salón es requerido")]
        [Display(Name = "Nombre del Salón")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string NombreSalon { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El NIT/RUC es requerido")]
        [Display(Name = "NIT/RUC")]
        [StringLength(20, ErrorMessage = "El NIT/RUC no puede exceder los 20 caracteres")]
        public string NitRuc { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La dirección es requerida")]
        [Display(Name = "Dirección")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder los 200 caracteres")]
        public string Direccion { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El teléfono es requerido")]
        [Display(Name = "Teléfono del Salón")]
        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        public string Telefono { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email del Salón")]
        public string? Email { get; set; }
        
        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }
        
        [Display(Name = "Logo del Salón")]
        public string? LogoUrl { get; set; }
        
        [Display(Name = "Horario de Atención")]
        [StringLength(100)]
        public string? HorarioAtencion { get; set; }
        
        // Relación con el usuario dueño
        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
        
        // Relación con solicitudes de contrato
        public ICollection<SolicitudContrato>? SolicitudesRecibidas { get; set; }
        
        // Relación con servicios del salón
        public ICollection<Servicio>? Servicios { get; set; }
        
        // Relación con suscripción actual (navegación directa)
        public SalonSuscripcion? SuscripcionActual { get; set; }
        
        // Propiedad de navegación para acceder a las suscripciones históricas
        [NotMapped]
        [Display(Name = "Suscripciones")]
        public IEnumerable<SalonSuscripcion>? Suscripciones => 
            new[] { SuscripcionActual }.Where(s => s != null);
    }
}
