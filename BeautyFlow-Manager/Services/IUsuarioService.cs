using BeautyFlow_Manager.Models;

namespace BeautyFlow_Manager.Services
{
    public interface IUsuarioService
    {
        Task<Usuario?> ObtenerPorIdAsync(Guid id);
        Task<List<Usuario>> ObtenerTodosAsync();
        Task<bool> AsignarRolAsync(Guid usuarioId, Guid rolId);
    }
}
