using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models.ViewModels;

/// <summary>
/// Kryteria wyszukiwania wolnych sal (wymaganie 5) – te same pola
/// służą do filtrowania listy sal i kalendarza (wymaganie 4).
/// </summary>
public class RoomSearchViewModel
{
    [Display(Name = "Data")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    [Display(Name = "Od godziny")]
    [DataType(DataType.Time)]
    public TimeSpan? FromTime { get; set; }

    [Display(Name = "Do godziny")]
    [DataType(DataType.Time)]
    public TimeSpan? ToTime { get; set; }

    [Display(Name = "Budynek")]
    public int? BuildingId { get; set; }

    [Display(Name = "Piętro")]
    public int? Floor { get; set; }

    [Display(Name = "Minimalna pojemność")]
    public int? MinCapacity { get; set; }

    [Display(Name = "Typ sali")]
    public RoomType? Type { get; set; }

    [Display(Name = "Dostępność")]
    public RoomStatus? Status { get; set; }

    [Display(Name = "Wymagane wyposażenie")]
    public List<int> EquipmentIds { get; set; } = new();

    [Display(Name = "Szukaj (nazwa lub numer)")]
    public string Query { get; set; }

    /// <summary>Wyniki wyszukiwania – wypełniane przez kontroler.</summary>
    public List<Room> Results { get; set; } = new();

    public List<Building> Buildings { get; set; } = new();
    public List<Equipment> AllEquipment { get; set; } = new();

    /// <summary>Czy użytkownik podał komplet danych do sprawdzania wolnych terminów.</summary>
    public bool HasTimeRange => Date.HasValue && FromTime.HasValue && ToTime.HasValue;

    public DateTime? StartDateTime => HasTimeRange ? Date.Value.Date + FromTime.Value : null;
    public DateTime? EndDateTime => HasTimeRange ? Date.Value.Date + ToTime.Value : null;
}
