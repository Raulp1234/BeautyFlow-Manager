using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeautyFlow_Manager.Models;
using BeautyFlow_Manager.Data;

namespace BeautyFlow_Manager.Helpers
{
    /// <summary>
    /// Clase utilitaria para completar datos de usuarios creados por seeder
    /// Esto simula el flujo normal de registro de salón/trabajador
    /// </summary>
    public static class SeedDataCompleter
    {
        public static async Task CompleteSalonDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();
            
            // Buscar el usuario con email salon@beautyflow.com
            var salonUser = await userManager.FindByEmailAsync("salon@beautyflow.com");
            
            if (salonUser == null)
            {
                Console.WriteLine("No se encontró el usuario salon@beautyflow.com");
                return;
            }
            
            // Verificar si ya tiene un salón registrado
            var existingSalon = await context.Salones.FirstOrDefaultAsync(s => s.UsuarioId == salonUser.Id);
            
            if (existingSalon != null)
            {
                Console.WriteLine($"El usuario {salonUser.Email} ya tiene un salón registrado: {existingSalon.NombreSalon}");
                return;
            }
            
            // Crear el salón con datos de prueba (simulando el flujo normal)
            var salon = new Salon
            {
                Id = Guid.NewGuid(),
                NombreSalon = "Belleza & Estilo",
                NitRuc = "20123456789",
                Direccion = "Av. Principal 123, Ciudad Moda",
                Telefono = "+51 987 654 321",
                Email = "contacto@belezayestilo.com",
                Descripcion = "Salón de belleza especializado en cortes modernos, coloración y tratamientos capilares. Ambiente acogedor y profesionales certificados.",
                HorarioAtencion = "Lunes a Sábado 9:00 AM - 8:00 PM",
                UsuarioId = salonUser.Id,
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };
            
            context.Salones.Add(salon);
            await context.SaveChangesAsync();
            
            // Actualizar el rol del usuario a "Salon" (quitando el rol anterior si existe)
            var currentRoles = await userManager.GetRolesAsync(salonUser);
            if (currentRoles.Any())
            {
                await userManager.RemoveFromRolesAsync(salonUser, currentRoles);
            }
            await userManager.AddToRoleAsync(salonUser, "Salon");
            
            // Actualizar el RolId en la tabla de usuarios
            var salonRoleId = DbInitializer.SalonRoleId;
            salonUser.RolId = salonRoleId;
            await userManager.UpdateAsync(salonUser);
            
            Console.WriteLine($"✅ Salón creado exitosamente para {salonUser.Email}");
            Console.WriteLine($"   Nombre del Salón: {salon.NombreSalon}");
            Console.WriteLine($"   NIT/RUC: {salon.NitRuc}");
            Console.WriteLine($"   Dirección: {salon.Direccion}");
            Console.WriteLine($"   Teléfono: {salon.Telefono}");
            Console.WriteLine($"   Rol actualizado a: Salon");
        }
        
        public static async Task CompleteTrabajadorDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();
            
            // Buscar el usuario con email trabajador@beautyflow.com
            var trabajadorUser = await userManager.FindByEmailAsync("trabajador@beautyflow.com");
            
            if (trabajadorUser == null)
            {
                Console.WriteLine("No se encontró el usuario trabajador@beautyflow.com");
                return;
            }
            
            // Verificar si ya tiene un perfil de trabajador registrado
            var existingTrabajador = await context.TrabajadoresIndependientes.FirstOrDefaultAsync(t => t.UsuarioId == trabajadorUser.Id);
            
            if (existingTrabajador != null)
            {
                Console.WriteLine($"El usuario {trabajadorUser.Email} ya tiene un perfil de trabajador registrado: {existingTrabajador.NombreCompleto}");
                return;
            }
            
            // Crear el perfil de trabajador independiente con datos de prueba
            var trabajador = new TrabajadorIndependiente
            {
                Id = Guid.NewGuid(),
                NombreCompleto = "María González",
                DocumentoIdentidad = "12345678",
                EspecialidadPrincipal = "Peluquería",
                OtrasEspecialidades = "Manicure, Maquillaje",
                Telefono = "+51 912 345 678",
                EmailProfesional = "maria.gonzalez@beautyflow.com",
                Descripcion = "Profesional con 5 años de experiencia en cortes modernos, coloración y estilismo. Apasionada por hacer resaltar la belleza natural de cada cliente.",
                AniosExperiencia = 5,
                PrecioReferencial = 50.00m,
                UsuarioId = trabajadorUser.Id,
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };
            
            context.TrabajadoresIndependientes.Add(trabajador);
            await context.SaveChangesAsync();
            
            // Actualizar el rol del usuario a "Trabajador"
            var currentRoles = await userManager.GetRolesAsync(trabajadorUser);
            if (currentRoles.Any())
            {
                await userManager.RemoveFromRolesAsync(trabajadorUser, currentRoles);
            }
            await userManager.AddToRoleAsync(trabajadorUser, "Trabajador");
            
            // Actualizar el RolId en la tabla de usuarios
            var trabajadorRoleId = DbInitializer.TrabajadorRoleId;
            trabajadorUser.RolId = trabajadorRoleId;
            await userManager.UpdateAsync(trabajadorUser);
            
            Console.WriteLine($"✅ Perfil de trabajador creado exitosamente para {trabajadorUser.Email}");
            Console.WriteLine($"   Nombre: {trabajador.NombreCompleto}");
            Console.WriteLine($"   Especialidad: {trabajador.EspecialidadPrincipal}");
            Console.WriteLine($"   Experiencia: {trabajador.AniosExperiencia} años");
            Console.WriteLine($"   Rol actualizado a: Trabajador");
        }
        
        public static async Task CompleteAllSeedDataAsync(IServiceProvider serviceProvider)
        {
            Console.WriteLine("🔄 Completando datos de usuarios creados por seeder...\n");
            
            await CompleteSalonDataAsync(serviceProvider);
            Console.WriteLine();
            await CompleteTrabajadorDataAsync(serviceProvider);
            
            Console.WriteLine("\n✅ Proceso completado exitosamente!");
        }
    }
}
