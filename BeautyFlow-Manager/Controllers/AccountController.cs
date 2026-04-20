using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Models.ViewModels;
using BeautyFlow_Manager.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            var roles = await _rolService.ObtenerTodosAsync();
            var viewModel = new RegisterViewModel
            {
                RolesDisponibles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nombre
                }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Usuario
                {
                    Id = Guid.NewGuid(),
                    UserName = model.Email,
                    Email = model.Email,
                    NombreCompleto = model.NombreCompleto,
                    Telefono = model.Telefono,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true,
                    RolId = model.RolSeleccionado
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Asignar rol seleccionado
                    var rol = await _rolService.ObtenerPorIdAsync(model.RolSeleccionado);
                    if (rol != null)
                    {
                        await _userManager.AddToRoleAsync(user, rol.Nombre);
                    }

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

            // Recargar roles si hay error
            var roles = await _rolService.ObtenerTodosAsync();
            model.RolesDisponibles = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nombre
            }).ToList();

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
