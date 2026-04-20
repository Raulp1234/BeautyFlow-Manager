using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class PerfilPublicoController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PerfilPublicoController> _logger;

        public PerfilPublicoController(
            UserManager<Usuario> userManager,
            ApplicationDbContext context,
            ILogger<PerfilPublicoController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> CrearPerfil()
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

            // Verificar si ya tiene un perfil de salón o trabajador
            var yaTieneSalon = await _context.Salones.AnyAsync(s => s.UsuarioId == userId);
            var yaTieneTrabajador = await _context.TrabajadoresIndependientes.AnyAsync(t => t.UsuarioId == userId);

            if (yaTieneSalon || yaTieneTrabajador)
            {
                ViewBag.YaTienePerfil = true;
                ViewBag.TipoPerfilExistente = yaTieneSalon ? "Salón" : "Trabajador Independiente";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearPerfil(PerfilPublicoViewModel model)
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
                _logger.LogInformation("Usuario {Email} solicitó crear perfil: {TipoPerfil}", 
                    user.Email, model.TipoPerfil);
                
                // Redirigir a la vista correspondiente según el tipo de perfil
                if (model.TipoPerfil == "Salon")
                {
                    return RedirectToAction("CrearPerfilSalon");
                }
                else if (model.TipoPerfil == "TrabajadorIndependiente")
                {
                    return RedirectToAction("CrearPerfilTrabajador");
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CrearPerfilSalon()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verificar si ya tiene un salón registrado
            var yaTieneSalon = await _context.Salones.AnyAsync(s => s.UsuarioId == userId);
            if (yaTieneSalon)
            {
                TempData["Error"] = "Ya tienes un salón registrado.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new SalonViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CrearPerfilSalon(SalonViewModel model)
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
                    // Crear el salón
                    var salon = new Salon
                    {
                        NombreSalon = model.NombreSalon,
                        NitRuc = model.NitRuc,
                        Direccion = model.Direccion,
                        Telefono = model.Telefono,
                        Email = model.Email,
                        Descripcion = model.Descripcion,
                        HorarioAtencion = model.HorarioAtencion,
                        UsuarioId = userId,
                        FechaRegistro = DateTime.UtcNow,
                        Activo = true
                    };

                    _context.Salones.Add(salon);
                    await _context.SaveChangesAsync();

                    // Actualizar el rol del usuario a "Salon"
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    await _userManager.AddToRoleAsync(user, "Salon");

                    TempData["Success"] = "¡Salón registrado exitosamente! Ahora puedes gestionar tu negocio.";
                    return RedirectToAction("Index", "Dashboard");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al registrar el salón: " + ex.Message);
                    _logger.LogError(ex, "Error al registrar salón para usuario {UserId}", userId);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CrearPerfilTrabajador()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verificar si ya tiene un perfil de trabajador registrado
            var yaTieneTrabajador = await _context.TrabajadoresIndependientes.AnyAsync(t => t.UsuarioId == userId);
            if (yaTieneTrabajador)
            {
                TempData["Error"] = "Ya tienes un perfil de trabajador registrado.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new TrabajadorIndependienteViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CrearPerfilTrabajador(TrabajadorIndependienteViewModel model)
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
                    // Crear el perfil de trabajador independiente
                    var trabajador = new TrabajadorIndependiente
                    {
                        NombreCompleto = model.NombreCompleto,
                        DocumentoIdentidad = model.DocumentoIdentidad,
                        EspecialidadPrincipal = model.EspecialidadPrincipal,
                        OtrasEspecialidades = model.OtrasEspecialidades,
                        Telefono = model.Telefono,
                        EmailProfesional = model.EmailProfesional,
                        Descripcion = model.Descripcion,
                        AniosExperiencia = model.AniosExperiencia,
                        PrecioReferencial = model.PrecioReferencial,
                        UsuarioId = userId,
                        FechaRegistro = DateTime.UtcNow,
                        Activo = true
                    };

                    _context.TrabajadoresIndependientes.Add(trabajador);
                    await _context.SaveChangesAsync();

                    // Actualizar el rol del usuario a "Trabajador"
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    await _userManager.AddToRoleAsync(user, "Trabajador");

                    TempData["Success"] = "¡Perfil de trabajador independiente registrado exitosamente!";
                    return RedirectToAction("Index", "Dashboard");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al registrar el perfil: " + ex.Message);
                    _logger.LogError(ex, "Error al registrar trabajador independiente para usuario {UserId}", userId);
                }
            }

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

    public class PerfilPublicoViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un tipo de perfil")]
        [Display(Name = "Tipo de Perfil")]
        public string TipoPerfil { get; set; } = string.Empty;
    }

    public class SalonViewModel
    {
        [Required(ErrorMessage = "El nombre del salón es requerido")]
        [Display(Name = "Nombre del Salón")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string NombreSalon { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El NIT/RUC es requerido")]
        [Display(Name = "NIT/RUC")]
        [StringLength(20, ErrorMessage = "El NIT/RUC no puede exceder los 20 caracteres")]
        public string NitRuc { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La dirección es requerida")]
        [Display(Name = "Dirección")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder los 200 caracteres")]
        public string Direccion { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El teléfono es requerido")]
        [Display(Name = "Teléfono del Salón")]
        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        public string Telefono { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email del Salón")]
        public string? Email { get; set; }
        
        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }
        
        [Display(Name = "Horario de Atención")]
        [StringLength(100)]
        public string? HorarioAtencion { get; set; }
    }

    public class TrabajadorIndependienteViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name = "Nombre Completo")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string NombreCompleto { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El documento de identidad es requerido")]
        [Display(Name = "Documento de Identidad (DNI/CE)")]
        [StringLength(20, ErrorMessage = "El documento no puede exceder los 20 caracteres")]
        public string DocumentoIdentidad { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La especialidad es requerida")]
        [Display(Name = "Especialidad Principal")]
        [StringLength(100, ErrorMessage = "La especialidad no puede exceder los 100 caracteres")]
        public string EspecialidadPrincipal { get; set; } = string.Empty;
        
        [Display(Name = "Otras Especialidades")]
        [StringLength(300)]
        public string? OtrasEspecialidades { get; set; }
        
        [Required(ErrorMessage = "El teléfono es requerido")]
        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        public string Telefono { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email Profesional")]
        public string? EmailProfesional { get; set; }
        
        [Display(Name = "Descripción / Biografía")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }
        
        [Display(Name = "Experiencia (años)")]
        [Range(0, 50, ErrorMessage = "Ingrese una experiencia válida entre 0 y 50 años")]
        public int? AniosExperiencia { get; set; }
        
        [Display(Name = "Precio Referencial")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioReferencial { get; set; }
    }
}
