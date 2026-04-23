using Microsoft.AspNetCore.Identity;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Data;
using Microsoft.EntityFrameworkCore;

namespace BeautyFlow_Manager.Helpers
{
    public static class DbInitializer
    {
        // GUIDs predefinidos para los roles
        public static readonly Guid ManagerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid SalonRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid TrabajadorRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ClienteRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();

            // Asegurar que la base de datos está creada
            await context.Database.EnsureCreatedAsync();

            // Crear roles con GUIDs fijos en Identity
            var roles = new List<(Guid Id, string Name, string Description)>
            {
                (ManagerRoleId, "Manager", "Administrador global"),
                (SalonRoleId, "Salon", "Dueño del salón"),
                (TrabajadorRoleId, "Trabajador", "Empleado del salón"),
                (ClienteRoleId, "Cliente", "Cliente regular")
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    var identityRole = new IdentityRole<Guid>(role.Name)
                    {
                        Id = role.Id,
                        Name = role.Name,
                        NormalizedName = role.Name.ToUpper()
                    };

                    var result = await roleManager.CreateAsync(identityRole);
                    if (!result.Succeeded)
                    {
                        Console.WriteLine($"Error al crear rol {role.Name}: {string.Join(", ", result.Errors)}");
                    }
                }
            }

            // También crear roles en la tabla personalizada Roles
            if (!context.Roles.Any())
            {
                foreach (var role in roles)
                {
                    context.Roles.Add(new Rol
                    {
                        Id = role.Id,
                        Nombre = role.Name,
                        Descripcion = role.Description
                    });
                }
                await context.SaveChangesAsync();
            }

            // Crear usuario admin
            var adminEmail = "admin@beautyflow.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                var admin = new Usuario
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                    NombreCompleto = "Administrador Principal",
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true,
                    EmailConfirmed = true,
                    PhoneNumber = "+1234567890"
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Manager");
                    Console.WriteLine("Usuario admin creado exitosamente");
                }
                else
                {
                    Console.WriteLine($"Error al crear admin: {string.Join(", ", result.Errors)}");
                }
            }

            // Crear usuarios de prueba para cada rol
            await CreateTestUser(userManager, "salon@beautyflow.com", "Salon123!", "Dueño Salón", "Salon", SalonRoleId);
            await CreateTestUser(userManager, "trabajador@beautyflow.com", "Trabajador123!", "Empleado Salón", "Trabajador", TrabajadorRoleId);
            await CreateTestUser(userManager, "cliente@beautyflow.com", "Cliente123!", "Cliente Regular", "Cliente", ClienteRoleId);
            
            // Inicializar tipos de suscripción
            await InitializeSuscripciones(context);
        }
        
        private static async Task InitializeSuscripciones(ApplicationDbContext context)
        {
            if (context.TiposSuscripcion.Any())
                return;
                
            var suscripciones = new List<TipoSuscripcion>
            {
                new TipoSuscripcion
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
                    Nombre = "FREE",
                    Descripcion = "Plan gratuito básico para salones pequeños",
                    PrecioMensual = 0.00m,
                    PrecioAnual = 0.00m,
                    DuracionDias = 30,
                    MaxTrabajadores = 3,
                    MaxServicios = 10,
                    ComisionPorcentaje = 5.00m,
                    PermiteMultiplesUbicaciones = false,
                    IncluyeReportesAvanzados = false,
                    IncluyeSoportePrioritario = false,
                    IncluyePersonalizacionMarca = false,
                    Activo = true,
                    EsPlanDefecto = true
                },
                new TipoSuscripcion
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
                    Nombre = "BASIC",
                    Descripcion = "Plan básico con características esenciales",
                    PrecioMensual = 29.99m,
                    PrecioAnual = 299.99m,
                    DuracionDias = 30,
                    MaxTrabajadores = 10,
                    MaxServicios = 50,
                    ComisionPorcentaje = 3.00m,
                    PermiteMultiplesUbicaciones = false,
                    IncluyeReportesAvanzados = true,
                    IncluyeSoportePrioritario = false,
                    IncluyePersonalizacionMarca = false,
                    Activo = true,
                    EsPlanDefecto = false
                },
                new TipoSuscripcion
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555553"),
                    Nombre = "PREMIUM",
                    Descripcion = "Plan completo con todas las características",
                    PrecioMensual = 59.99m,
                    PrecioAnual = 599.99m,
                    DuracionDias = 30,
                    MaxTrabajadores = null, // Ilimitado
                    MaxServicios = null, // Ilimitado
                    ComisionPorcentaje = 1.00m,
                    PermiteMultiplesUbicaciones = true,
                    IncluyeReportesAvanzados = true,
                    IncluyeSoportePrioritario = true,
                    IncluyePersonalizacionMarca = true,
                    Activo = true,
                    EsPlanDefecto = false
                }
            };
            
            context.TiposSuscripcion.AddRange(suscripciones);
            await context.SaveChangesAsync();
        }

        private static async Task CreateTestUser(UserManager<Usuario> userManager, string email, string password, string nombre, string roleName, Guid roleId)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var newUser = new Usuario
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    NombreCompleto = nombre,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true,
                    EmailConfirmed = true,
                    RolId = roleId
                };

                var result = await userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, roleName);
                    Console.WriteLine($"Usuario {roleName} creado exitosamente");
                }
            }
        }
    }
}
