using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Data;
using BeautyFlow_Manager.Models;
using System.Security.Claims;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize]
    public class TrabajadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrabajadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trabajador/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var trabajador = await _context.TrabajadoresIndependientes
                .FirstOrDefaultAsync(t => t.UsuarioId == Guid.Parse(userId));

            if (trabajador == null)
                return RedirectToAction("CrearPerfil", "Trabajador");

            // Obtener servicios en los que trabaja
            var serviciosTrabajador = await _context.ServiciosTrabajadores
                .Include(st => st.Servicio)
                    .ThenInclude(s => s.Salon)
                .Where(st => st.TrabajadorId == trabajador.Id && st.Activo)
                .ToListAsync();

            // Obtener reservas próximas
            var reservasProximas = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Servicio)
                    .ThenInclude(s => s.Salon)
                .Where(r => r.TrabajadorId == trabajador.Id && r.FechaHoraInicio >= DateTime.UtcNow && r.Estado != EstadoReserva.Cancelada)
                .OrderBy(r => r.FechaHoraInicio)
                .Take(5)
                .ToListAsync();

            ViewBag.Trabajador = trabajador;
            ViewBag.Servicios = serviciosTrabajador;
            ViewBag.ReservasProximas = reservasProximas;
            ViewBag.TotalServicios = serviciosTrabajador.Count;
            ViewBag.TotalReservasProximas = reservasProximas.Count;

            return View();
        }

        // GET: Trabajador/CrearPerfil
        public IActionResult CrearPerfil()
        {
            return View();
        }

        // POST: Trabajador/CrearPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPerfil(TrabajadorIndependiente model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            // Verificar que el usuario no tenga ya un perfil de trabajador
            var trabajadorExistente = await _context.TrabajadoresIndependientes
                .FirstOrDefaultAsync(t => t.UsuarioId == Guid.Parse(userId));
            
            if (trabajadorExistente != null)
            {
                ModelState.AddModelError("", "Ya tienes un perfil de trabajador registrado.");
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.UsuarioId = Guid.Parse(userId);
            model.FechaRegistro = DateTime.UtcNow;
            model.Activo = true;

            _context.TrabajadoresIndependientes.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Trabajador");
        }

        // GET: Trabajador/MisServicios
        public async Task<IActionResult> MisServicios()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trabajador = await _context.TrabajadoresIndependientes
                .FirstOrDefaultAsync(t => t.UsuarioId == Guid.Parse(userId));

            if (trabajador == null)
                return RedirectToAction("CrearPerfil", "Trabajador");

            var servicios = await _context.ServiciosTrabajadores
                .Include(st => st.Servicio)
                    .ThenInclude(s => s.Salon)
                .Where(st => st.TrabajadorId == trabajador.Id && st.Activo)
                .OrderBy(st => st.Servicio.Nombre)
                .ToListAsync();

            ViewBag.Trabajador = trabajador;
            return View(servicios);
        }

        // GET: Trabajador/MisReservas
        public async Task<IActionResult> MisReservas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trabajador = await _context.TrabajadoresIndependientes
                .FirstOrDefaultAsync(t => t.UsuarioId == Guid.Parse(userId));

            if (trabajador == null)
                return RedirectToAction("CrearPerfil", "Trabajador");

            var reservas = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Servicio)
                    .ThenInclude(s => s.Salon)
                .Where(r => r.TrabajadorId == trabajador.Id)
                .OrderByDescending(r => r.FechaHoraInicio)
                .ToListAsync();

            ViewBag.Trabajador = trabajador;
            return View(reservas);
        }

        // GET: Trabajador/EditarPerfil
        public async Task<IActionResult> EditarPerfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trabajador = await _context.TrabajadoresIndependientes
                .FirstOrDefaultAsync(t => t.UsuarioId == Guid.Parse(userId));

            if (trabajador == null)
                return NotFound();

            return View(trabajador);
        }

        // POST: Trabajador/EditarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(TrabajadorIndependiente model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trabajador = await _context.TrabajadoresIndependientes
                .FirstOrDefaultAsync(t => t.UsuarioId == Guid.Parse(userId));

            if (trabajador == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            trabajador.NombreCompleto = model.NombreCompleto;
            trabajador.DocumentoIdentidad = model.DocumentoIdentidad;
            trabajador.EspecialidadPrincipal = model.EspecialidadPrincipal;
            trabajador.OtrasEspecialidades = model.OtrasEspecialidades;
            trabajador.Telefono = model.Telefono;
            trabajador.EmailProfesional = model.EmailProfesional;
            trabajador.Descripcion = model.Descripcion;
            trabajador.AniosExperiencia = model.AniosExperiencia;
            trabajador.PrecioReferencial = model.PrecioReferencial;

            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Trabajador");
        }
    }
}
