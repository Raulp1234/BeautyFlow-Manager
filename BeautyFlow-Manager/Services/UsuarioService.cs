using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;

namespace BeautyFlow_Manager.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UserManager<Usuario> _userManager;

        public UsuarioService(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Usuario?> ObtenerPorIdAsync(Guid id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<List<Usuario>> ObtenerTodosAsync()
        {
            var usuarios = new List<Usuario>();
            // Nota: Para obtener todos los usuarios, necesitamos usar el contexto directamente
            // o implementar un método personalizado en el UserManager
            return usuarios;
        }

        public async Task<bool> AsignarRolAsync(Guid usuarioId, Guid rolId)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
            if (usuario == null)
                return false;

            // Obtener el nombre del rol desde la tabla Roles personalizada
            // y asignarlo usando Identity
            return true;
        }
    }
}
