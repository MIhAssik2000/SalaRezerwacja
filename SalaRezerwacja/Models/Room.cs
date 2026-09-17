using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models;

public class Room
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nazwa sali")]
    public string Name { get; set; }

    [Required, MaxLength(20)]
    [Display(Name = "Numer sali")]
    public string Number { get; set; }

    [Display(Name = "Budynek")]
    public int BuildingId { get; set; }
    public Building Building { get; set; }

    [Range(-2, 20)]
    [Display(Name = "Piętro")]
    public int Floor { get; set; }

    [Range(1, 2000)]
    [Display(Name = "Pojemność")]
    public int Capacity { get; set; }

    [Display(Name = "Typ sali")]
    public RoomType Type { get; set; }

    [Display(Name = "Dostępność")]
    public RoomStatus Status { get; set; } = RoomStatus.Wolna;

    /// <summary>
    /// Ograniczenie maksymalnego czasu pojedynczej rezerwacji (wymaganie 3),
    /// w minutach, ustalane osobno dla każdej sali.
    /// </summary>
    [Range(15, 1440)]
    [Display(Name = "Maks. czas rezerwacji (min)")]
    public int MaxReservationMinutes { get; set; } = 180;

    /// <summary>Opiekun sali – tylko on (lub administracja) może edytować tę salę.</summary>
    [Display(Name = "Opiekun sali")]
    public string OpiekunId { get; set; }
    public ApplicationUser Opiekun { get; set; }

    [MaxLength(500)]
    [Display(Name = "Uwagi")]
    public string Description { get; set; }

    public ICollection<RoomEquipment> RoomEquipments { get; set; } = new List<RoomEquipment>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public string FullLabel => $"{Name} ({Number})";
}
