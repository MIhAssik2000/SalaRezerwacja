using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Models;

namespace SalaRezerwacja.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Building> Buildings { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<RoomEquipment> RoomEquipments { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Klucz złożony dla tabeli łączącej sale z wyposażeniem.
        builder.Entity<RoomEquipment>()
            .HasKey(re => new { re.RoomId, re.EquipmentId });

        builder.Entity<RoomEquipment>()
            .HasOne(re => re.Room)
            .WithMany(r => r.RoomEquipments)
            .HasForeignKey(re => re.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RoomEquipment>()
            .HasOne(re => re.Equipment)
            .WithMany(e => e.RoomEquipments)
            .HasForeignKey(re => re.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Usunięcie opiekuna nie może kasować sali – dlatego Restrict.
        builder.Entity<Room>()
            .HasOne(r => r.Opiekun)
            .WithMany()
            .HasForeignKey(r => r.OpiekunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Room>()
            .HasOne(r => r.Building)
            .WithMany(b => b.Rooms)
            .HasForeignKey(r => r.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Reservation>()
            .HasOne(r => r.Room)
            .WithMany(room => room.Reservations)
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indeks przyspieszający wykrywanie kolizji terminów.
        builder.Entity<Reservation>()
            .HasIndex(r => new { r.RoomId, r.StartTime, r.EndTime });

        builder.Entity<Equipment>()
            .HasIndex(e => e.Name)
            .IsUnique();
    }
}
