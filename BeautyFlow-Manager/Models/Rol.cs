using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Models
{
    public class Rol
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Descripcion { get; set; }
    }
}
