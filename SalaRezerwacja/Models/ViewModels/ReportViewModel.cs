namespace SalaRezerwacja.Models.ViewModels;

/// <summary>Jeden wiersz raportu wykorzystania sal (wymaganie 6).</summary>
public class RoomUsageRow
{
    public int RoomId { get; set; }
    public string RoomName { get; set; }
    public string RoomNumber { get; set; }
    public string BuildingName { get; set; }
    public int Capacity { get; set; }
    public int ReservationCount { get; set; }
    public int CancelledCount { get; set; }
    public double TotalHours { get; set; }
    public double AverageHours { get; set; }

    /// <summary>Procent wykorzystania względem dostępnych godzin roboczych w okresie.</summary>
    public double UtilizationPercent { get; set; }
}

/// <summary>Model widoku raportów: filtr okresu + wyniki + statystyki zbiorcze.</summary>
public class ReportViewModel
{
    public DateTime From { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime To { get; set; } = DateTime.Today.AddDays(1);
    public int? BuildingId { get; set; }

    public List<RoomUsageRow> Rows { get; set; } = new();
    public List<Models.Building> Buildings { get; set; } = new();

    public int TotalReservations => Rows.Sum(r => r.ReservationCount);
    public int TotalCancelled => Rows.Sum(r => r.CancelledCount);
    public double TotalHours => Math.Round(Rows.Sum(r => r.TotalHours), 1);
    public RoomUsageRow MostUsed => Rows.OrderByDescending(r => r.TotalHours).FirstOrDefault();
    public RoomUsageRow LeastUsed => Rows.OrderBy(r => r.TotalHours).FirstOrDefault();
}
