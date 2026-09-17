using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models.ViewModels;

/// <summary>Formularz rezerwacji sali (wymaganie 3).</summary>
public class ReservationCreateViewModel : IValidatableObject
{
    [Display(Name = "Sala")]
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz salę.")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Podaj cel rezerwacji."), MaxLength(150)]
    [Display(Name = "Cel rezerwacji")]
    public string Title { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Data")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Godzina rozpoczęcia")]
    public TimeSpan FromTime { get; set; } = new(8, 0, 0);

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Godzina zakończenia")]
    public TimeSpan ToTime { get; set; } = new(9, 30, 0);

    public List<Room> Rooms { get; set; } = new();

    public DateTime StartDateTime => Date.Date + FromTime;
    public DateTime EndDateTime => Date.Date + ToTime;

    /// <summary>Walidacja, której nie da się wyrazić atrybutami.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ToTime <= FromTime)
            yield return new ValidationResult(
                "Godzina zakończenia musi być późniejsza niż rozpoczęcia.",
                new[] { nameof(ToTime) });

        if (StartDateTime < DateTime.Now)
            yield return new ValidationResult(
                "Nie można rezerwować terminu w przeszłości.",
                new[] { nameof(Date) });
    }
}
