using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models;

public class Building
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nazwa budynku")]
    public string Name { get; set; }

    [MaxLength(200)]
    [Display(Name = "Adres")]
    public string Address { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
