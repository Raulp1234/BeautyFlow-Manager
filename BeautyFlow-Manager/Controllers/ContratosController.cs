using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize]
    public class ContratosController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContratosController> _logger;

        public ContratosController(UserManager<Usuario> userManager, ApplicationDbContext context, ILogger<ContratosController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> VerSalonesDisponibles()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Trabajador")) return RedirectToAction("Index", "Dashboard");
            var salones = await _context.Salones.Include(s => s.Usuario).Where(s => s.Activo).ToListAsync();
            var trabajador = await _context.TrabajadoresIndependientes.FirstOrDefaultAsync(t => t.UsuarioId == userId);
            ViewBag.TieneContratoActivo = trabajador?.ContratoActualId != null;
            ViewBag.TrabajadorId = trabajador?.Id;
            return View(salones);
        }

        [HttpGet]
        public async Task<IActionResult> EnviarSolicitud(Guid salonId)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Trabajador")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.Include(s => s.Usuario).FirstOrDefaultAsync(s => s.Id == salonId && s.Activo);
            if (salon == null) { TempData["Error"] = "Salon no encontrado"; return RedirectToAction("VerSalonesDisponibles"); }
            var trabajador = await _context.TrabajadoresIndependientes.FirstOrDefaultAsync(t => t.UsuarioId == userId);
            if (trabajador == null) { TempData["Error"] = "No tienes perfil de trabajador"; return RedirectToAction("Index", "Dashboard"); }
            if (trabajador.ContratoActualId.HasValue) { TempData["Error"] = "Ya tienes contrato activo"; return RedirectToAction("MisSolicitudes"); }
            var solicitudExistente = await _context.SolicitudesContrato.AnyAsync(s => s.TrabajadorId == trabajador.Id && s.SalonId == salonId && s.Estado == EstadoSolicitud.Pendiente);
            if (solicitudExistente) { TempData["Error"] = "Ya tienes solicitud pendiente para este salon"; return RedirectToAction("MisSolicitudes"); }
            var model = new EnviarSolicitudViewModel { SalonId = salonId, NombreSalon = salon.NombreSalon, DireccionSalon = salon.Direccion, TelefonoSalon = salon.Telefono, DueñoSalon = salon.Usuario?.NombreCompleto ?? "Desconocido" };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarSolicitud(EnviarSolicitudViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                try
                {
                    var trabajador = await _context.TrabajadoresIndependientes.FirstOrDefaultAsync(t => t.UsuarioId == userId);
                    if (trabajador == null || trabajador.ContratoActualId.HasValue) { TempData["Error"] = "No puedes enviar solicitudes"; return RedirectToAction("MisSolicitudes"); }
                    var solicitud = new SolicitudContrato { TrabajadorId = trabajador.Id, SalonId = model.SalonId, Estado = EstadoSolicitud.Pendiente, MensajeTrabajador = model.MensajeTrabajador, FechaSolicitud = DateTime.UtcNow };
                    _context.SolicitudesContrato.Add(solicitud);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Solicitud enviada exitosamente.";
                    return RedirectToAction("MisSolicitudes");
                }
                catch (Exception ex) { ModelState.AddModelError("", "Error: " + ex.Message); }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MisSolicitudes()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Trabajador")) return RedirectToAction("Index", "Dashboard");
            var trabajador = await _context.TrabajadoresIndependientes.FirstOrDefaultAsync(t => t.UsuarioId == userId);
            if (trabajador == null) { TempData["Error"] = "No tienes perfil de trabajador"; return RedirectToAction("Index", "Dashboard"); }
            var solicitudes = await _context.SolicitudesContrato.Include(s => s.Salon).ThenInclude(s => s.Usuario).Where(s => s.TrabajadorId == trabajador.Id).OrderByDescending(s => s.FechaSolicitud).ToListAsync();
            ViewBag.TieneContratoActivo = trabajador.ContratoActualId.HasValue;
            return View(solicitudes);
        }

        [HttpGet]
        public async Task<IActionResult> SolicitudesRecibidas()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Salon")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Activo);
            if (salon == null) { TempData["Error"] = "No tienes salon registrado"; return RedirectToAction("Index", "Dashboard"); }
            var solicitudes = await _context.SolicitudesContrato.Include(s => s.Trabajador).ThenInclude(t => t.Usuario).Where(s => s.SalonId == salon.Id).OrderByDescending(s => s.FechaSolicitud).ToListAsync();
            ViewBag.SalonId = salon.Id;
            ViewBag.PendientesCount = solicitudes.Count(s => s.Estado == EstadoSolicitud.Pendiente);
            return View(solicitudes);
        }

        [HttpGet]
        public async Task<IActionResult> VerDetalleSolicitud(Guid solicitudId)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Salon")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Activo);
            if (salon == null) return RedirectToAction("Index", "Dashboard");
            var solicitud = await _context.SolicitudesContrato.Include(s => s.Trabajador).ThenInclude(t => t.Usuario).FirstOrDefaultAsync(s => s.Id == solicitudId && s.SalonId == salon.Id);
            if (solicitud == null) { TempData["Error"] = "Solicitud no encontrada"; return RedirectToAction("SolicitudesRecibidas"); }
            return View(solicitud);
        }

        [HttpPost]
        public async Task<IActionResult> AceptarSolicitud(Guid solicitudId)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Salon")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Activo);
            if (salon == null) return RedirectToAction("Index", "Dashboard");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var solicitud = await _context.SolicitudesContrato.Include(s => s.Trabajador).FirstOrDefaultAsync(s => s.Id == solicitudId && s.SalonId == salon.Id);
                if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente || solicitud.Trabajador.ContratoActualId.HasValue) { TempData["Error"] = "No se puede aceptar"; return RedirectToAction("SolicitudesRecibidas"); }
                solicitud.Estado = EstadoSolicitud.Aceptada;
                solicitud.FechaRespuesta = DateTime.UtcNow;
                solicitud.Trabajador.ContratoActualId = solicitud.Id;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Solicitud aceptada.";
                return RedirectToAction("SolicitudesRecibidas");
            }
            catch (Exception ex) { await transaction.RollbackAsync(); TempData["Error"] = "Error: " + ex.Message; return RedirectToAction("SolicitudesRecibidas"); }
        }

        [HttpPost]
        public async Task<IActionResult> RechazarSolicitud(Guid solicitudId, string? motivo)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Salon")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Activo);
            if (salon == null) return RedirectToAction("Index", "Dashboard");
            try
            {
                var solicitud = await _context.SolicitudesContrato.FirstOrDefaultAsync(s => s.Id == solicitudId && s.SalonId == salon.Id);
                if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente) { TempData["Error"] = "No se puede rechazar"; return RedirectToAction("SolicitudesRecibidas"); }
                solicitud.Estado = EstadoSolicitud.Rechazada;
                solicitud.FechaRespuesta = DateTime.UtcNow;
                solicitud.MotivoRechazo = motivo;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Solicitud rechazada.";
                return RedirectToAction("SolicitudesRecibidas");
            }
            catch (Exception ex) { TempData["Error"] = "Error: " + ex.Message; return RedirectToAction("SolicitudesRecibidas"); }
        }

        [HttpGet]
        public async Task<IActionResult> GestionarTrabajadores()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Salon")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Activo);
            if (salon == null) { TempData["Error"] = "No tienes salon registrado"; return RedirectToAction("Index", "Dashboard"); }
            var trabajadores = await _context.SolicitudesContrato.Include(s => s.Trabajador).ThenInclude(t => t.Usuario).Where(s => s.SalonId == salon.Id && s.Estado == EstadoSolicitud.Aceptada && s.Trabajador.ContratoActualId == s.Id).Select(s => s.Trabajador).ToListAsync();
            ViewBag.SalonId = salon.Id;
            ViewBag.SalonName = salon.NombreSalon;
            return View(trabajadores);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarContrato(Guid trabajadorId)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Salon")) return RedirectToAction("Index", "Dashboard");
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == userId && s.Activo);
            if (salon == null) return RedirectToAction("Index", "Dashboard");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var contrato = await _context.SolicitudesContrato.Include(s => s.Trabajador).FirstOrDefaultAsync(s => s.SalonId == salon.Id && s.TrabajadorId == trabajadorId && s.Estado == EstadoSolicitud.Aceptada);
                if (contrato == null) { TempData["Error"] = "Contrato no encontrado"; return RedirectToAction("GestionarTrabajadores"); }
                contrato.Estado = EstadoSolicitud.Finalizada;
                contrato.FechaRespuesta = DateTime.UtcNow;
                contrato.Trabajador.ContratoActualId = null;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Contrato finalizado.";
                return RedirectToAction("GestionarTrabajadores");
            }
            catch (Exception ex) { await transaction.RollbackAsync(); TempData["Error"] = "Error: " + ex.Message; return RedirectToAction("GestionarTrabajadores"); }
        }

        [HttpPost]
        public async Task<IActionResult> CancelarContrato()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return RedirectToAction("Login", "Account");
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Trabajador")) return RedirectToAction("Index", "Dashboard");
            var trabajador = await _context.TrabajadoresIndependientes.FirstOrDefaultAsync(t => t.UsuarioId == userId);
            if (trabajador == null || !trabajador.ContratoActualId.HasValue) { TempData["Error"] = "No tienes contrato activo"; return RedirectToAction("MisSolicitudes"); }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var contrato = await _context.SolicitudesContrato.FirstOrDefaultAsync(s => s.Id == trabajador.ContratoActualId.Value && s.Estado == EstadoSolicitud.Aceptada);
                if (contrato == null) { TempData["Error"] = "Contrato no encontrado"; return RedirectToAction("MisSolicitudes"); }
                contrato.Estado = EstadoSolicitud.Cancelada;
                contrato.FechaRespuesta = DateTime.UtcNow;
                trabajador.ContratoActualId = null;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Contrato cancelado.";
                return RedirectToAction("MisSolicitudes");
            }
            catch (Exception ex) { await transaction.RollbackAsync(); TempData["Error"] = "Error: " + ex.Message; return RedirectToAction("MisSolicitudes"); }
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && Guid.TryParse(claim.Value, out var userId)) return userId;
            return Guid.Empty;
        }
    }

    public class EnviarSolicitudViewModel
    {
        public Guid SalonId { get; set; }
        [Display(Name = "Salon")] public string NombreSalon { get; set; } = string.Empty;
        [Display(Name = "Direccion")] public string DireccionSalon { get; set; } = string.Empty;
        [Display(Name = "Telefono")] public string TelefonoSalon { get; set; } = string.Empty;
        [Display(Name = "Dueno del Salon")] public string DuenoSalon { get; set; } = string.Empty;
        [Display(Name = "Mensaje para el dueno")] [StringLength(500)] public string? MensajeTrabajador { get; set; }
    }
}
