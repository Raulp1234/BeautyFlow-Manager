using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class PerfilPublicoController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<PerfilPublicoController> _logger;

        public PerfilPublicoController(
            UserManager<Usuario> userManager,
            ILogger<PerfilPublicoController> logger)
        {
            _userManager = userManager;
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
            // Aquí iría la lógica para verificar si ya existe un perfil asociado
            
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
                // Aquí iría la lógica para crear el perfil de Salón o Trabajador Independiente
                // y actualizar el rol del usuario
                
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
        public IActionResult CrearPerfilSalon()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CrearPerfilTrabajador()
        {
            return View();
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
}
