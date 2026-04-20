using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.ComponentModel.DataAnnotations;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ClienteController> _logger;

        public ClienteController(
            UserManager<Usuario> userManager,
            IWebHostEnvironment environment,
            ILogger<ClienteController> logger)
        {
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
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
