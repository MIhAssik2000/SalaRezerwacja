using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models.ViewModels;

/// <summary>Formularz dodawania i edycji sali (wymaganie 2).</summary>
public class RoomFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Podaj nazwę sali."), MaxLength(100)]
    [Display(Name = "Nazwa sali")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Podaj numer sali."), MaxLength(20)]
    [Display(Name = "Numer sali")]
    public string Number { get; set; }

    [Display(Name = "Budynek")]
    public int BuildingId { get; set; }

    [Range(-2, 20)]
    [Display(Name = "Piętro")]
    public int Floor { get; set; }

    [Range(1, 2000, ErrorMessage = "Pojemność musi być z zakresu 1–2000.")]
    [Display(Name = "Pojemność")]
    public int Capacity { get; set; }

    [Display(Name = "Typ sali")]
    public RoomType Type { get; set; }

    [Display(Name = "Dostępność")]
    public RoomStatus Status { get; set; }

    [Range(15, 1440, ErrorMessage = "Limit rezerwacji: od 15 do 1440 minut.")]
    [Display(Name = "Maks. czas rezerwacji (min)")]
    public int MaxReservationMinutes { get; set; } = 180;

    [Display(Name = "Opiekun sali")]
    public string OpiekunId { get; set; }

    [MaxLength(500)]
    [Display(Name = "Uwagi")]
    public string Description { get; set; }

    [Display(Name = "Wyposażenie")]
    public List<int> EquipmentIds { get; set; } = new();

    public List<Building> Buildings { get; set; } = new();
    public List<Equipment> AllEquipment { get; set; } = new();
    public List<ApplicationUser> Opiekunowie { get; set; } = new();
}
