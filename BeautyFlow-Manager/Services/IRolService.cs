using BeautyFlow_Manager.Models;

namespace BeautyFlow_Manager.Services
{
    public interface IRolService
    {
        Task<List<Rol>> ObtenerTodosAsync();
        Task<Rol?> ObtenerPorIdAsync(Guid id);
        Task<Rol?> ObtenerPorNombreAsync(string nombre);
    }
}
