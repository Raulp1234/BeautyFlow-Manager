using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Data;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ClienteController> _logger;

        public ClienteController(
            UserManager<Usuario> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ClienteController> logger)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: Cliente/BuscarSalones
        [AllowAnonymous]
        public async Task<IActionResult> BuscarSalones(string searchTerm, string categoria)
        {
            var query = _context.Salones
                .Include(s => s.Servicios)
                .Where(s => s.Activo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.NombreSalon.Contains(searchTerm) || 
                                        s.Direccion.Contains(searchTerm) ||
                                        s.Descripcion.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(s => s.Servicios.Any(serv => serv.Categoria == categoria && serv.Activo));
            }

            var salones = await query.ToListAsync();
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Categoria = categoria;
            
            // Obtener categorías disponibles
            ViewBag.Categorias = await _context.Servicios
                .Where(s => s.Activo && !string.IsNullOrEmpty(s.Categoria))
                .Select(s => s.Categoria)
                .Distinct()
                .ToListAsync();

            return View(salones);
        }

        // GET: Cliente/VerSalon/5
        [AllowAnonymous]
        public async Task<IActionResult> VerSalon(Guid id)
        {
            var salon = await _context.Salones
                .Include(s => s.Servicios)
                    .ThenInclude(serv => serv.ServiciosTrabajadores)
                        .ThenInclude(st => st.Trabajador)
                .FirstOrDefaultAsync(s => s.Id == id && s.Activo);

            if (salon == null)
                return NotFound();

            // Obtener suscripción actual del salón
            var suscripcionActual = await _context.SalonesSuscripciones
                .Include(ss => ss.TipoSuscripcion)
                .Where(ss => ss.SalonId == id && ss.Estado == EstadoSuscripcion.Activa)
                .OrderByDescending(ss => ss.FechaInicio)
                .FirstOrDefaultAsync();

            ViewBag.SuscripcionActual = suscripcionActual;
            return View(salon);
        }

        // GET: Cliente/ServicioDetalle/5
        [AllowAnonymous]
        public async Task<IActionResult> ServicioDetalle(Guid id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.Salon)
                .Include(s => s.ServiciosTrabajadores)
                    .ThenInclude(st => st.Trabajador)
                .FirstOrDefaultAsync(s => s.Id == id && s.Activo);

            if (servicio == null)
                return NotFound();

            return View(servicio);
        }

        // GET: Cliente/Reservar
        [AllowAnonymous]
        public async Task<IActionResult> Reservar(Guid servicioId, Guid? trabajadorId)
        {
            var servicio = await _context.Servicios
                .Include(s => s.Salon)
                .Include(s => s.ServiciosTrabajadores)
                    .ThenInclude(st => st.Trabajador)
                .FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);

            if (servicio == null)
                return NotFound();

            // Si no se especificó trabajador, mostrar todos los disponibles para este servicio
            List<TrabajadorIndependiente> trabajadoresDisponibles;
            if (trabajadorId.HasValue)
            {
                var trabajador = await _context.TrabajadoresIndependientes.FindAsync(trabajadorId.Value);
                trabajadoresDisponibles = new List<TrabajadorIndependiente> { trabajador };
            }
            else
            {
                trabajadoresDisponibles = servicio.ServiciosTrabajadores
                    .Where(st => st.Activo)
                    .Select(st => st.Trabajador)
                    .ToList();
            }

            ViewBag.TrabajadoresDisponibles = trabajadoresDisponibles;
            ViewBag.TrabajadorSeleccionado = trabajadorId;
            ViewBag.Servicio = servicio;

            return View();
        }

        // POST: Cliente/Reservar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(Reserva model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var servicio = await _context.Servicios
                .Include(s => s.Salon)
                .FirstOrDefaultAsync(s => s.Id == model.ServicioId && s.Activo);

            if (servicio == null)
            {
                ModelState.AddModelError("", "El servicio no está disponible.");
                return View(model);
            }

            // Verificar disponibilidad del trabajador en el horario solicitado
            var hayConflicto = await _context.Reservas
                .AnyAsync(r => r.TrabajadorId == model.TrabajadorId &&
                              r.FechaHoraInicio <= model.FechaHoraFin &&
                              r.FechaHoraFin >= model.FechaHoraInicio &&
                              r.Estado != EstadoReserva.Cancelada);

            if (hayConflicto)
            {
                ModelState.AddModelError("", "El trabajador no está disponible en ese horario.");
                var servicioCompleto = await _context.Servicios
                    .Include(s => s.ServiciosTrabajadores)
                        .ThenInclude(st => st.Trabajador)
                    .FirstOrDefaultAsync(s => s.Id == model.ServicioId);
                ViewBag.TrabajadoresDisponibles = servicioCompleto.ServiciosTrabajadores
                    .Where(st => st.Activo)
                    .Select(st => st.Trabajador)
                    .ToList();
                ViewBag.Servicio = servicioCompleto;
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.ClienteId = Guid.Parse(userId);
            model.SalonId = servicio.SalonId;
            model.CostoTotal = servicio.Costo;
            model.FechaCreacion = DateTime.UtcNow;
            model.Estado = EstadoReserva.Pendiente;
            model.EstadoPago = EstadoPago.Pendiente;

            _context.Reservas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("MisReservas", "Cliente");
        }

        // GET: Cliente/MisReservas
        public async Task<IActionResult> MisReservas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var reservas = await _context.Reservas
                .Include(r => r.Servicio)
                    .ThenInclude(s => s.Salon)
                .Include(r => r.Trabajador)
                .Where(r => r.ClienteId == Guid.Parse(userId))
                .OrderByDescending(r => r.FechaHoraInicio)
                .ToListAsync();

            return View(reservas);
        }

        // POST: Cliente/CancelarReserva/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReserva(Guid id, string motivo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id && r.ClienteId == Guid.Parse(userId));

            if (reserva == null)
                return NotFound();

            if (reserva.Estado == EstadoReserva.Confirmada || reserva.Estado == EstadoReserva.Pendiente)
            {
                reserva.Estado = EstadoReserva.Cancelada;
                reserva.MotivoCancelacion = motivo;
                reserva.FechaCancelacion = DateTime.UtcNow;
                reserva.FechaUltimaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MisReservas", "Cliente");
        }

        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new ClientePerfilViewModel
            {
                NombreCompleto = user.NombreCompleto,
                Telefono = user.Telefono ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FotoUrl = user.FotoUrl ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> MiPerfil(ClientePerfilViewModel model, IFormFile? FotoFile)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Actualizar datos básicos
                    user.NombreCompleto = model.NombreCompleto;
                    user.Telefono = model.Telefono;

                    // Manejar subida de foto
                    if (FotoFile != null && FotoFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "perfiles");
                        
                        // Crear directorio si no existe
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Generar nombre único para el archivo
                        var uniqueFileName = $"{userId}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(FotoFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await FotoFile.CopyToAsync(stream);
                        }

                        // Guardar la ruta relativa en la base de datos
                        user.FotoUrl = "/uploads/perfiles/" + uniqueFileName;
                    }

                    // Actualizar usuario en la base de datos
                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        TempData["Success"] = "¡Perfil actualizado exitosamente!";
                        _logger.LogInformation("Usuario {Email} actualizó su perfil", user.Email);
                        return RedirectToAction("MiPerfil");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar el perfil: " + ex.Message);
                    _logger.LogError(ex, "Error al actualizar perfil para usuario {UserId}", userId);
                }
            }

            // Si llegamos aquí, hubo un error, mostrar el formulario nuevamente
            model.Email = user.Email ?? string.Empty;
            return View(model);
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && Guid.TryParse(claim.Value, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }

    public class ClientePerfilViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name = "Nombre Completo")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Foto de Perfil")]
        public string FotoUrl { get; set; } = string.Empty;
    }
}
