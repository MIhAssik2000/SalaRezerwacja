using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models;

/// <summary>Słownik wyposażenia: projektor, tablica, sprzęt multimedialny itd.</summary>
public class Equipment
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    [Display(Name = "Wyposażenie")]
    public string Name { get; set; }

    public ICollection<RoomEquipment> RoomEquipments { get; set; } = new List<RoomEquipment>();
}

/// <summary>Tabela łącząca sale z wyposażeniem (relacja wiele-do-wielu).</summary>
public class RoomEquipment
{
    public int RoomId { get; set; }
    public Room Room { get; set; }

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; }
}
