using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Data;
using BeautyFlow_Manager.Models;
using System.Security.Claims;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize(Roles = "Manager")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Dashboard principal del Admin
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalSalones = await _context.Salones.CountAsync(s => s.Activo),
                TotalTrabajadores = await _context.TrabajadoresIndependientes.CountAsync(t => t.Activo),
                TotalUsuarios = await _context.Users.CountAsync(u => u.Activo),
                TotalReservasHoy = await _context.Reservas.CountAsync(r => r.FechaHoraInicio.Date == DateTime.UtcNow.Date),
                SalonesPorSuscripcion = await _context.SalonesSuscripciones
                    .GroupBy(ss => ss.TipoSuscripcion!.Nombre)
                    .Select(g => new SuscripcionCountDto
                    {
                        NombrePlan = g.Key ?? "Sin Plan",
                        Cantidad = g.Count()
                    })
                    .ToListAsync(),
                IngresosMes = await _context.SalonesSuscripciones
                    .Where(ss => ss.Estado == EstadoSuscripcion.Activa)
                    .SumAsync(ss => ss.TipoSuscripcion!.PrecioMensual)
            };

            return View(viewModel);
        }

        // Gestión de Suscripciones - Listar todos los planes
        public async Task<IActionResult> Suscripciones()
        {
            var suscripciones = await _context.TiposSuscripcion
                .OrderBy(s => s.PrecioMensual)
                .ToListAsync();
            return View(suscripciones);
        }

        // Crear nueva suscripción
        public IActionResult CrearSuscripcion()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearSuscripcion(TipoSuscripcion modelo)
        {
            if (ModelState.IsValid)
            {
                modelo.Id = Guid.NewGuid();
                modelo.Activo = true;
                _context.Add(modelo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Suscripción creada exitosamente";
                return RedirectToAction(nameof(Suscripciones));
            }
            return View(modelo);
        }

        // Editar suscripción
        public async Task<IActionResult> EditarSuscripcion(Guid? id)
        {
            if (id == null) return NotFound();
            
            var suscripcion = await _context.TiposSuscripcion.FindAsync(id);
            if (suscripcion == null) return NotFound();
            
            return View(suscripcion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarSuscripcion(Guid id, TipoSuscripcion modelo)
        {
            if (id != modelo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(modelo);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Suscripción actualizada exitosamente";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TiposSuscripcion.Any(e => e.Id == modelo.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Suscripciones));
            }
            return View(modelo);
        }

        // Gestionar Salones - Ver todos los salones
        public async Task<IActionResult> Salones(int? page, string? searchTerm)
        {
            var query = _context.Salones
                .Include(s => s.Usuario)
                .Include(s => s.SuscripcionActual)
                    .ThenInclude(ss => ss!.TipoSuscripcion)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.NombreSalon.Contains(searchTerm) || 
                                        s.NitRuc.Contains(searchTerm) ||
                                        s.Usuario!.NombreCompleto.Contains(searchTerm));
            }

            var pageNumber = page ?? 1;
            var pageSize = 20;
            
            var model = new SalonListViewModel
            {
                Salones = await query.OrderByDescending(s => s.FechaRegistro)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                TotalCount = await query.CountAsync(),
                CurrentPage = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        // Ver detalle de un salón
        public async Task<IActionResult> DetalleSalon(Guid? id)
        {
            if (id == null) return NotFound();
            
            var salon = await _context.Salones
                .Include(s => s.Usuario)
                .Include(s => s.SuscripcionActual)
                    .ThenInclude(ss => ss!.TipoSuscripcion)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (salon == null) return NotFound();
            
            var viewModel = new AdminSalonDetailViewModel
            {
                Salon = salon,
                HistorialSuscripciones = await _context.SalonesSuscripciones
                    .Where(ss => ss.SalonId == id)
                    .Include(ss => ss.TipoSuscripcion)
                    .OrderByDescending(ss => ss.FechaInicio)
                    .Take(10)
                    .ToListAsync(),
                TotalReservas = await _context.Reservas.CountAsync(r => r.SalonId == id),
                TotalServicios = await _context.Servicios.CountAsync(s => s.SalonId == id)
            };
            
            return View(viewModel);
        }

        // Asignar suscripción a un salón
        public async Task<IActionResult> AsignarSuscripcion(Guid? salonId)
        {
            if (salonId == null) return NotFound();
            
            var salon = await _context.Salones.FindAsync(salonId);
            if (salon == null) return NotFound();
            
            var viewModel = new AsignarSuscripcionViewModel
            {
                SalonId = salonId.Value,
                SalonNombre = salon.NombreSalon,
                SuscripcionesDisponibles = await _context.TiposSuscripcion
                    .Where(s => s.Activo)
                    .OrderBy(s => s.PrecioMensual)
                    .ToListAsync(),
                SuscripcionActual = await _context.SalonesSuscripciones
                    .Include(ss => ss.TipoSuscripcion)
                    .FirstOrDefaultAsync(ss => ss.SalonId == salonId && ss.Estado == EstadoSuscripcion.Activa)
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarSuscripcion(AsignarSuscripcionViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                var salon = await _context.Salones.FindAsync(modelo.SalonId);
                if (salon == null) return NotFound();
                
                var tipoSuscripcion = await _context.TiposSuscripcion.FindAsync(modelo.TipoSuscripcionId);
                if (tipoSuscripcion == null) return NotFound();
                
                // Desactivar suscripción anterior si existe
                var suscripcionAnterior = await _context.SalonesSuscripciones
                    .FirstOrDefaultAsync(ss => ss.SalonId == modelo.SalonId && ss.Estado == EstadoSuscripcion.Activa);
                    
                if (suscripcionAnterior != null)
                {
                    suscripcionAnterior.Estado = EstadoSuscripcion.Cancelada;
                    suscripcionAnterior.FechaFin = DateTime.UtcNow;
                    suscripcionAnterior.FechaUltimaActualizacion = DateTime.UtcNow;
                }
                
                // Crear nueva suscripción
                var nuevaSuscripcion = new SalonSuscripcion
                {
                    Id = Guid.NewGuid(),
                    SalonId = modelo.SalonId,
                    TipoSuscripcionId = modelo.TipoSuscripcionId,
                    FechaInicio = DateTime.UtcNow,
                    FechaFin = DateTime.UtcNow.AddDays(tipoSuscripcion.DuracionDias),
                    FechaProximoPago = DateTime.UtcNow.AddDays(tipoSuscripcion.DuracionDias),
                    Estado = modelo.EsPrueba ? EstadoSuscripcion.Prueba : EstadoSuscripcion.Activa,
                    MetodoPago = "Administrativo",
                    UsuarioRegistroId = GetUserId(),
                    NotasAdministrativas = modelo.Notas
                };
                
                _context.SalonesSuscripciones.Add(nuevaSuscripcion);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Suscripción asignada exitosamente";
                return RedirectToAction(nameof(DetalleSalon), new { id = modelo.SalonId });
            }
            
            // Recargar datos si hay error
            modelo.SuscripcionesDisponibles = await _context.TiposSuscripcion
                .Where(s => s.Activo)
                .OrderBy(s => s.PrecioMensual)
                .ToListAsync();
                
            return View(modelo);
        }

        // Gestionar Trabajadores Independientes
        public async Task<IActionResult> Trabajadores(int? page, string? searchTerm)
        {
            var query = _context.TrabajadoresIndependientes
                .Include(t => t.Usuario)
                .Include(t => t.ContratoActual)
                    .ThenInclude(c => c!.Salon)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.NombreCompleto.Contains(searchTerm) || 
                                        t.DocumentoIdentidad.Contains(searchTerm) ||
                                        t.EspecialidadPrincipal.Contains(searchTerm));
            }

            var pageNumber = page ?? 1;
            var pageSize = 20;
            
            var model = new TrabajadorListViewModel
            {
                Trabajadores = await query.OrderByDescending(t => t.FechaRegistro)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                TotalCount = await query.CountAsync(),
                CurrentPage = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        // Estadísticas y Reportes
        public async Task<IActionResult> Reportes()
        {
            var viewModel = new AdminReportesViewModel
            {
                ReservasPorEstado = await _context.Reservas
                    .GroupBy(r => r.Estado)
                    .Select(g => new EstadoCountDto
                    {
                        Estado = g.Key.ToString(),
                        Cantidad = g.Count()
                    })
                    .ToListAsync(),
                SalonesActivos = await _context.Salones.CountAsync(s => s.Activo),
                SalonesInactivos = await _context.Salones.CountAsync(s => !s.Activo),
                IngresosTotales = await _context.SalonesSuscripciones
                    .Where(ss => ss.Estado == EstadoSuscripcion.Activa)
                    .SumAsync(ss => ss.TipoSuscripcion!.PrecioMensual)
            };
            
            return View(viewModel);
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    // ViewModels para el AdminController
    public class AdminDashboardViewModel
    {
        public int TotalSalones { get; set; }
        public int TotalTrabajadores { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalReservasHoy { get; set; }
        public decimal IngresosMes { get; set; }
        public List<SuscripcionCountDto> SalonesPorSuscripcion { get; set; } = new();
    }

    public class SuscripcionCountDto
    {
        public string NombrePlan { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class SalonListViewModel
    {
        public List<Salon> Salones { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class AdminSalonDetailViewModel
    {
        public Salon Salon { get; set; } = new();
        public List<SalonSuscripcion> HistorialSuscripciones { get; set; } = new();
        public int TotalReservas { get; set; }
        public int TotalServicios { get; set; }
    }

    public class AsignarSuscripcionViewModel
    {
        public Guid SalonId { get; set; }
        public string SalonNombre { get; set; } = string.Empty;
        public Guid TipoSuscripcionId { get; set; }
        public bool EsPrueba { get; set; }
        public string? Notas { get; set; }
        public List<TipoSuscripcion> SuscripcionesDisponibles { get; set; } = new();
        public SalonSuscripcion? SuscripcionActual { get; set; }
    }

    public class TrabajadorListViewModel
    {
        public List<TrabajadorIndependiente> Trabajadores { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class AdminReportesViewModel
    {
        public List<EstadoCountDto> ReservasPorEstado { get; set; } = new();
        public int SalonesActivos { get; set; }
        public int SalonesInactivos { get; set; }
        public decimal IngresosTotales { get; set; }
    }

    public class EstadoCountDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
