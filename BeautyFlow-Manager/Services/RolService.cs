using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Data;
using BeautyFlow_Manager.Models;

namespace BeautyFlow_Manager.Services
{
    public class RolService : IRolService
    {
        private readonly ApplicationDbContext _context;

        public RolService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Rol>> ObtenerTodosAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<Rol?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<Rol?> ObtenerPorNombreAsync(string nombre)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre);
        }
    }
}
