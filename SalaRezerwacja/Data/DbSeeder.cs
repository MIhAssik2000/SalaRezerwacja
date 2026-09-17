using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Models;

namespace SalaRezerwacja.Data;

/// <summary>
/// Tworzy role, konta testowe i przykładowe dane przy pierwszym uruchomieniu.
/// Dzięki temu aplikacja po `Update-Database` od razu ma czym operować.
/// </summary>
public static class DbSeeder
{
    public const string DefaultPassword = "Haslo123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in new[] { Roles.Student, Roles.Opiekun, Roles.Administracja })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var admin = await EnsureUser(userManager, "admin@uczelnia.edu.pl", "Anna", "Administracka", Roles.Administracja);
        var opiekun = await EnsureUser(userManager, "opiekun@uczelnia.edu.pl", "Piotr", "Opiekuński", Roles.Opiekun);
        await EnsureUser(userManager, "student@uczelnia.edu.pl", "Jan", "Studencki", Roles.Student);

        if (!await db.Buildings.AnyAsync())
        {
            var a = new Building { Name = "Budynek A", Address = "ul. Uczelniana 1" };
            var b = new Building { Name = "Budynek B", Address = "ul. Uczelniana 3" };
            db.Buildings.AddRange(a, b);

            var projektor = new Equipment { Name = "Projektor" };
            var tablica = new Equipment { Name = "Tablica" };
            var multimedia = new Equipment { Name = "Sprzęt multimedialny" };
            var komputery = new Equipment { Name = "Stanowiska komputerowe" };
            var klimatyzacja = new Equipment { Name = "Klimatyzacja" };
            db.Equipment.AddRange(projektor, tablica, multimedia, komputery, klimatyzacja);
            await db.SaveChangesAsync();

            var rooms = new List<Room>
            {
                new() { Name = "Aula duża", Number = "A-101", Building = a, Floor = 1, Capacity = 250,
                        Type = RoomType.Wykladowa, MaxReservationMinutes = 480, OpiekunId = opiekun.Id,
                        Description = "Największa aula wykładowa." },
                new() { Name = "Sala seminaryjna", Number = "A-204", Building = a, Floor = 2, Capacity = 30,
                        Type = RoomType.Seminaryjna, MaxReservationMinutes = 180, OpiekunId = opiekun.Id },
                new() { Name = "Laboratorium chemiczne", Number = "B-015", Building = b, Floor = 0, Capacity = 18,
                        Type = RoomType.Laboratorium, MaxReservationMinutes = 240, OpiekunId = opiekun.Id,
                        Status = RoomStatus.Konserwacja },
                new() { Name = "Pracownia komputerowa", Number = "B-112", Building = b, Floor = 1, Capacity = 24,
                        Type = RoomType.Komputerowa, MaxReservationMinutes = 300, OpiekunId = opiekun.Id }
            };
            db.Rooms.AddRange(rooms);
            await db.SaveChangesAsync();

            db.RoomEquipments.AddRange(
                new RoomEquipment { RoomId = rooms[0].Id, EquipmentId = projektor.Id },
                new RoomEquipment { RoomId = rooms[0].Id, EquipmentId = multimedia.Id },
                new RoomEquipment { RoomId = rooms[1].Id, EquipmentId = tablica.Id },
                new RoomEquipment { RoomId = rooms[1].Id, EquipmentId = projektor.Id },
                new RoomEquipment { RoomId = rooms[3].Id, EquipmentId = komputery.Id },
                new RoomEquipment { RoomId = rooms[3].Id, EquipmentId = projektor.Id },
                new RoomEquipment { RoomId = rooms[3].Id, EquipmentId = klimatyzacja.Id });

            db.Reservations.Add(new Reservation
            {
                RoomId = rooms[0].Id,
                UserId = admin.Id,
                Title = "Inauguracja roku akademickiego",
                StartTime = DateTime.Today.AddDays(1).AddHours(10),
                EndTime = DateTime.Today.AddDays(1).AddHours(12)
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUser(
        UserManager<ApplicationUser> userManager, string email, string first, string last, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                UniversityId = Guid.NewGuid().ToString()[..8]
            };
            await userManager.CreateAsync(user, DefaultPassword);
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }
}
