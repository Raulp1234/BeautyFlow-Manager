using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Models
{
    public class TrabajadorIndependiente
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name = "Nombre Completo")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string NombreCompleto { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El documento de identidad es requerido")]
        [Display(Name = "Documento de Identidad (DNI/CE)")]
        [StringLength(20, ErrorMessage = "El documento no puede exceder los 20 caracteres")]
        public string DocumentoIdentidad { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La especialidad es requerida")]
        [Display(Name = "Especialidad Principal")]
        [StringLength(100, ErrorMessage = "La especialidad no puede exceder los 100 caracteres")]
        public string EspecialidadPrincipal { get; set; } = string.Empty;
        
        [Display(Name = "Otras Especialidades")]
        [StringLength(300)]
        public string? OtrasEspecialidades { get; set; }
        
        [Required(ErrorMessage = "El teléfono es requerido")]
        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        public string Telefono { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email Profesional")]
        public string? EmailProfesional { get; set; }
        
        [Display(Name = "Descripción / Biografía")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }
        
        [Display(Name = "Foto de Perfil")]
        public string? FotoUrl { get; set; }
        
        [Display(Name = "Experiencia (años)")]
        [Range(0, 50, ErrorMessage = "Ingrese una experiencia válida entre 0 y 50 años")]
        public int? AniosExperiencia { get; set; }
        
        [Display(Name = "Precio Referencial")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioReferencial { get; set; }
        
        // Relación con el usuario
        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
    }
}
