using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models;

public class Reservation
{
    public int Id { get; set; }

    [Display(Name = "Sala")]
    public int RoomId { get; set; }
    public Room Room { get; set; }

    [Display(Name = "Rezerwujący")]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "Cel rezerwacji")]
    public string Title { get; set; }

    [Display(Name = "Początek")]
    public DateTime StartTime { get; set; }

    [Display(Name = "Koniec")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Status")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Potwierdzona;

    public ReservationSource Source { get; set; } = ReservationSource.Uzytkownik;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CancelledAt { get; set; }

    /// <summary>Czy wysłano już przypomnienie o zbliżającej się rezerwacji.</summary>
    public bool ReminderSent { get; set; }

    public double DurationMinutes => (EndTime - StartTime).TotalMinutes;

    /// <summary>Anulować można tylko rezerwację, która jeszcze się nie rozpoczęła (wymaganie 3).</summary>
    public bool CanBeCancelled => Status == ReservationStatus.Potwierdzona && StartTime > DateTime.Now;
}
