using Microsoft.AspNetCore.Identity;

namespace BeautyFlow_Manager.Models
{
    public class Usuario : IdentityUser<Guid>
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public Guid RolId { get; set; }
        public bool Activo { get; set; } = true;
    }
}
