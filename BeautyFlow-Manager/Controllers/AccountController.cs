using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Models.ViewModels;
using BeautyFlow_Manager.Services;
using System.Security.Claims;

namespace BeautyFlow_Manager.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IRolService _rolService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            IRolService rolService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _rolService = rolService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!user.Activo)
                    {
                        ModelState.AddModelError(string.Empty, "Su cuenta está desactivada. Contacte al administrador.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email, 
                        model.Password, 
                        model.RememberMe, 
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Usuario inició sesión: {Email}", model.Email);
                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction(nameof(LoginWith2fa));
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Cuenta bloqueada: {Email}", model.Email);
                        return RedirectToAction(nameof(Lockout));
                    }
                }
                ModelState.AddModelError(string.Empty, "Email o contraseña inválidos.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Obtener solo el rol Cliente para asignación automática
            var clienteRole = await _rolService.ObtenerPorNombreAsync("Cliente");
            var viewModel = new RegisterViewModel();
            
            if (clienteRole != null)
            {
                // Establecer el rol Cliente por defecto (oculto)
                viewModel.RolSeleccionado = clienteRole.Id;
            }
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Forzar que el rol sea siempre Cliente en el registro público
                var clienteRole = await _rolService.ObtenerPorNombreAsync("Cliente");
                if (clienteRole == null)
                {
                    ModelState.AddModelError(string.Empty, "Error: No se encontró el rol Cliente. Contacte al administrador.");
                    return View(model);
                }

                var user = new Usuario
                {
                    Id = Guid.NewGuid(),
                    UserName = model.Email,
                    Email = model.Email,
                    NombreCompleto = model.NombreCompleto,
                    Telefono = model.Telefono,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true,
                    RolId = clienteRole.Id  // Siempre asignar rol Cliente
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Asignar rol Cliente
                    await _userManager.AddToRoleAsync(user, "Cliente");

                    _logger.LogInformation("Usuario creado: {Email}", user.Email);
                    
                    // Auto login después del registro
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuario cerró sesión");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LoginWith2fa()
        {
            return View();
        }
    }
}
