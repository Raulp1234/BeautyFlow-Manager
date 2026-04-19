using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;
using System.Security.Claims;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            UserManager<Usuario> userManager,
            ILogger<DashboardController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
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

            var role = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = role.FirstOrDefault() ?? "Sin rol";
            ViewBag.UserId = userId;
            ViewBag.UserName = user.NombreCompleto;
            ViewBag.UserEmail = user.Email;

            return View(user);
        }

        [HttpGet]
        public IActionResult Manager()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Salon()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Trabajador()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Cliente()
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
}
