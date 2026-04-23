using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Data;
using BeautyFlow_Manager.Models;
using System.Security.Claims;

namespace BeautyFlow_Manager.Controllers
{
    [Authorize]
    public class SalonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Salon/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var salon = await _context.Salones
                .FirstOrDefaultAsync(s => s.UsuarioId == Guid.Parse(userId));

            if (salon == null)
                return RedirectToAction("Crear", "Salon");

            // Obtener suscripción actual
            var suscripcionActual = await _context.SalonesSuscripciones
                .Include(s => s.TipoSuscripcion)
                .Where(s => s.SalonId == salon.Id && s.Estado == EstadoSuscripcion.Activa)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();

            // Métricas del salón
            var totalServicios = await _context.Servicios.CountAsync(s => s.SalonId == salon.Id && s.Activo);
            var totalTrabajadores = await _context.ServiciosTrabajadores
                .Include(st => st.Trabajador)
                .CountAsync(st => st.Servicio.SalonId == salon.Id && st.Activo);
            var totalReservas = await _context.Reservas.CountAsync(r => r.SalonId == salon.Id);
            var reservasPendientes = await _context.Reservas.CountAsync(r => r.SalonId == salon.Id && r.Estado == EstadoReserva.Pendiente);

            ViewBag.Salon = salon;
            ViewBag.SuscripcionActual = suscripcionActual;
            ViewBag.TotalServicios = totalServicios;
            ViewBag.TotalTrabajadores = totalTrabajadores;
            ViewBag.TotalReservas = totalReservas;
            ViewBag.ReservasPendientes = reservasPendientes;

            return View();
        }

        // GET: Salon/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Salon/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Salon model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            // Verificar que el usuario no tenga ya un salón
            var salonExistente = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == Guid.Parse(userId));
            if (salonExistente != null)
            {
                ModelState.AddModelError("", "Ya tienes un salón registrado.");
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.UsuarioId = Guid.Parse(userId);
            model.FechaRegistro = DateTime.UtcNow;
            model.Activo = true;

            _context.Salones.Add(model);
            await _context.SaveChangesAsync();

            // Asignar suscripción FREE por defecto
            var planFree = await _context.TiposSuscripcion.FirstOrDefaultAsync(t => t.EsPlanDefecto);
            if (planFree != null)
            {
                var suscripcion = new SalonSuscripcion
                {
                    Id = Guid.NewGuid(),
                    SalonId = model.Id,
                    TipoSuscripcionId = planFree.Id,
                    FechaInicio = DateTime.UtcNow,
                    FechaFin = DateTime.UtcNow.AddDays(planFree.DuracionDias),
                    FechaProximoPago = DateTime.UtcNow.AddDays(planFree.DuracionDias),
                    Estado = EstadoSuscripcion.Activa,
                    FechaUltimaActualizacion = DateTime.UtcNow
                };
                _context.SalonesSuscripciones.Add(suscripcion);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Dashboard", "Salon");
        }

        // GET: Salon/Servicios
        public async Task<IActionResult> Servicios()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == Guid.Parse(userId));

            if (salon == null)
                return RedirectToAction("Crear", "Salon");

            var servicios = await _context.Servicios
                .Include(s => s.ServiciosTrabajadores)
                    .ThenInclude(st => st.Trabajador)
                .Where(s => s.SalonId == salon.Id)
                .OrderBy(s => s.Nombre)
                .ToListAsync();

            ViewBag.Salon = salon;
            return View(servicios);
        }

        // GET: Salon/CrearServicio
        public async Task<IActionResult> CrearServicio()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == Guid.Parse(userId));

            if (salon == null)
                return RedirectToAction("Crear", "Salon");

            // Obtener trabajadores disponibles
            var trabajadores = await _context.TrabajadoresIndependientes.ToListAsync();
            ViewBag.SalonId = salon.Id;
            ViewBag.Trabajadores = new SelectList(trabajadores, "Id", "NombreCompleto");

            return View();
        }

        // POST: Salon/CrearServicio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearServicio(Servicio model, List<Guid> TrabajadorIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == Guid.Parse(userId));

            if (salon == null)
                return RedirectToAction("Crear", "Salon");

            if (!ModelState.IsValid)
            {
                var trabajadores = await _context.TrabajadoresIndependientes.ToListAsync();
                ViewBag.SalonId = salon.Id;
                ViewBag.Trabajadores = new SelectList(trabajadores, "Id", "NombreCompleto");
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.SalonId = salon.Id;
            model.FechaCreacion = DateTime.UtcNow;
            model.Activo = true;

            _context.Servicios.Add(model);
            await _context.SaveChangesAsync();

            // Asociar trabajadores al servicio
            if (TrabajadorIds != null && TrabajadorIds.Any())
            {
                foreach (var trabajadorId in TrabajadorIds)
                {
                    var servicioTrabajador = new ServicioTrabajador
                    {
                        Id = Guid.NewGuid(),
                        ServicioId = model.Id,
                        TrabajadorId = trabajadorId,
                        FechaAsignacion = DateTime.UtcNow,
                        Activo = true
                    };
                    _context.ServiciosTrabajadores.Add(servicioTrabajador);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Servicios", "Salon");
        }

        // GET: Salon/EditarServicio/5
        public async Task<IActionResult> EditarServicio(Guid id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.ServiciosTrabajadores)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null)
                return NotFound();

            var trabajadores = await _context.TrabajadoresIndependientes.ToListAsync();
            ViewBag.Trabajadores = new SelectList(trabajadores, "Id", "NombreCompleto");
            ViewBag.TrabajadorIds = servicio.ServiciosTrabajadores.Select(st => st.TrabajadorId).ToList();

            return View(servicio);
        }

        // POST: Salon/EditarServicio/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarServicio(Guid id, Servicio model, List<Guid> TrabajadorIds)
        {
            var servicio = await _context.Servicios.FindAsync(id);
            if (servicio == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var trabajadores = await _context.TrabajadoresIndependientes.ToListAsync();
                ViewBag.Trabajadores = new SelectList(trabajadores, "Id", "NombreCompleto");
                ViewBag.TrabajadorIds = TrabajadorIds;
                return View(model);
            }

            servicio.Nombre = model.Nombre;
            servicio.Descripcion = model.Descripcion;
            servicio.DuracionMinutos = model.DuracionMinutos;
            servicio.Costo = model.Costo;
            servicio.Categoria = model.Categoria;
            servicio.ImagenUrl = model.ImagenUrl;
            servicio.DiasAntelacionRequerida = model.DiasAntelacionRequerida;
            servicio.FechaUltimaActualizacion = DateTime.UtcNow;

            _context.Entry(servicio).State = EntityState.Modified;

            // Actualizar trabajadores asociados
            var serviciosTrabajadoresActuales = await _context.ServiciosTrabajadores
                .Where(st => st.ServicioId == id)
                .ToListAsync();

            // Eliminar los que ya no están seleccionados
            foreach (var st in serviciosTrabajadoresActuales)
            {
                if (!TrabajadorIds.Contains(st.TrabajadorId))
                {
                    st.Activo = false;
                }
            }

            // Agregar nuevos trabajadores
            if (TrabajadorIds != null)
            {
                foreach (var trabajadorId in TrabajadorIds)
                {
                    var existe = serviciosTrabajadoresActuales.Any(st => st.TrabajadorId == trabajadorId);
                    if (!existe)
                    {
                        var nuevoServicioTrabajador = new ServicioTrabajador
                        {
                            Id = Guid.NewGuid(),
                            ServicioId = id,
                            TrabajadorId = trabajadorId,
                            FechaAsignacion = DateTime.UtcNow,
                            Activo = true
                        };
                        _context.ServiciosTrabajadores.Add(nuevoServicioTrabajador);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Servicios", "Salon");
        }

        // GET: Salon/EliminarServicio/5
        public async Task<IActionResult> EliminarServicio(Guid id)
        {
            var servicio = await _context.Servicios.FindAsync(id);
            if (servicio == null)
                return NotFound();

            servicio.Activo = false;
            servicio.FechaUltimaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Servicios", "Salon");
        }

        // GET: Salon/Reservas
        public async Task<IActionResult> Reservas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var salon = await _context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == Guid.Parse(userId));

            if (salon == null)
                return RedirectToAction("Crear", "Salon");

            var reservas = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Servicio)
                .Include(r => r.Trabajador)
                .Where(r => r.SalonId == salon.Id)
                .OrderByDescending(r => r.FechaHoraInicio)
                .ToListAsync();

            ViewBag.Salon = salon;
            return View(reservas);
        }

        // POST: Salon/ConfirmarReserva/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarReserva(Guid id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
                return NotFound();

            reserva.Estado = EstadoReserva.Confirmada;
            reserva.FechaConfirmacion = DateTime.UtcNow;
            reserva.FechaUltimaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Reservas", "Salon");
        }

        // POST: Salon/CancelarReserva/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReserva(Guid id, string motivo)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
                return NotFound();

            reserva.Estado = EstadoReserva.Cancelada;
            reserva.MotivoCancelacion = motivo;
            reserva.FechaCancelacion = DateTime.UtcNow;
            reserva.FechaUltimaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Reservas", "Salon");
        }

        // GET: Salon/AsignarTrabajadores/5
        public async Task<IActionResult> AsignarTrabajadores(Guid id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.ServiciosTrabajadores)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null)
                return NotFound();

            var trabajadoresDisponibles = await _context.TrabajadoresIndependientes.ToListAsync();
            var trabajadorIdsActuales = servicio.ServiciosTrabajadores
                .Where(st => st.Activo)
                .Select(st => st.TrabajadorId)
                .ToList();

            ViewBag.TrabajadoresDisponibles = trabajadoresDisponibles;
            ViewBag.TrabajadorIdsActuales = trabajadorIdsActuales;

            return View(servicio);
        }

        // POST: Salon/AsignarTrabajadores/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarTrabajadores(Guid id, List<Guid> trabajadorIds)
        {
            var servicio = await _context.Servicios
                .Include(s => s.ServiciosTrabajadores)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null)
                return NotFound();

            // Obtener asignaciones actuales
            var asignacionesActuales = await _context.ServiciosTrabajadores
                .Where(st => st.ServicioId == id)
                .ToListAsync();

            // Desactivar las que no están en la nueva selección
            foreach (var asignacion in asignacionesActuales)
            {
                if (!trabajadorIds.Contains(asignacion.TrabajadorId))
                {
                    asignacion.Activo = false;
                }
            }

            // Agregar nuevas asignaciones
            if (trabajadorIds != null)
            {
                foreach (var trabajadorId in trabajadorIds)
                {
                    var existe = asignacionesActuales.Any(st => st.TrabajadorId == trabajadorId && st.Activo);
                    if (!existe)
                    {
                        var nuevaAsignacion = new ServicioTrabajador
                        {
                            Id = Guid.NewGuid(),
                            ServicioId = id,
                            TrabajadorId = trabajadorId,
                            FechaAsignacion = DateTime.UtcNow,
                            Activo = true
                        };
                        _context.ServiciosTrabajadores.Add(nuevaAsignacion);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Servicios", "Salon");
        }

        // GET: Salon/VerReservas
        public async Task<IActionResult> VerReservas(Guid servicioId)
        {
            var servicio = await _context.Servicios.FindAsync(servicioId);
            if (servicio == null)
                return NotFound();

            var reservas = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Servicio)
                .Include(r => r.Trabajador)
                .Where(r => r.ServicioId == servicioId)
                .OrderByDescending(r => r.FechaHoraInicio)
                .ToListAsync();

            ViewBag.ServicioNombre = servicio.Nombre;

            return View(reservas);
        }
    }
}
