using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;
using SalaRezerwacja.Models.ViewModels;

namespace SalaRezerwacja.Services;

public interface IReportService
{
    Task<ReportViewModel> BuildAsync(DateTime from, DateTime to, int? buildingId);
    byte[] ToCsv(ReportViewModel report);
    byte[] ToPdf(ReportViewModel report);
}

/// <summary>
/// WYMAGANIE 6 – raporty wykorzystania sal, eksport CSV/PDF i statystyki.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    /// <summary>Godziny pracy uczelni – podstawa do liczenia procentu wykorzystania.</summary>
    private const int WorkingHoursPerDay = 12; // 7:00–19:00

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<ReportViewModel> BuildAsync(DateTime from, DateTime to, int? buildingId)
    {
        var model = new ReportViewModel
        {
            From = from,
            To = to,
            BuildingId = buildingId,
            Buildings = await _db.Buildings.OrderBy(b => b.Name).ToListAsync()
        };

        var roomsQuery = _db.Rooms.Include(r => r.Building).AsQueryable();
        if (buildingId.HasValue)
            roomsQuery = roomsQuery.Where(r => r.BuildingId == buildingId);

        var rooms = await roomsQuery.OrderBy(r => r.Building.Name).ThenBy(r => r.Number).ToListAsync();
        var roomIds = rooms.Select(r => r.Id).ToList();

        var reservations = await _db.Reservations
            .Where(r => roomIds.Contains(r.RoomId) && r.StartTime < to && r.EndTime > from)
            .ToListAsync();

        var workingDays = CountWorkingDays(from, to);
        var availableHours = Math.Max(1, workingDays * WorkingHoursPerDay);

        foreach (var room in rooms)
        {
            var forRoom = reservations.Where(r => r.RoomId == room.Id).ToList();
            var confirmed = forRoom.Where(r => r.Status == ReservationStatus.Potwierdzona).ToList();
            var hours = confirmed.Sum(r => (r.EndTime - r.StartTime).TotalHours);

            model.Rows.Add(new RoomUsageRow
            {
                RoomId = room.Id,
                RoomName = room.Name,
                RoomNumber = room.Number,
                BuildingName = room.Building?.Name,
                Capacity = room.Capacity,
                ReservationCount = confirmed.Count,
                CancelledCount = forRoom.Count(r => r.Status == ReservationStatus.Anulowana),
                TotalHours = Math.Round(hours, 1),
                AverageHours = confirmed.Count == 0 ? 0 : Math.Round(hours / confirmed.Count, 1),
                UtilizationPercent = Math.Round(hours / availableHours * 100, 1)
            });
        }

        model.Rows = model.Rows.OrderByDescending(r => r.TotalHours).ToList();
        return model;
    }

    private static int CountWorkingDays(DateTime from, DateTime to)
    {
        var days = 0;
        for (var d = from.Date; d < to.Date; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Sunday)
                days++;
        return Math.Max(days, 1);
    }

    public byte[] ToCsv(ReportViewModel report)
    {
        using var memory = new MemoryStream();
        // UTF-8 z BOM, żeby polskie znaki poprawnie otwierały się w Excelu.
        using (var writer = new StreamWriter(memory, new UTF8Encoding(true)))
        using (var csv = new CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.GetCultureInfo("pl-PL")) { Delimiter = ";" }))
        {
            csv.WriteField("Budynek");
            csv.WriteField("Sala");
            csv.WriteField("Numer");
            csv.WriteField("Pojemnosc");
            csv.WriteField("Liczba rezerwacji");
            csv.WriteField("Anulowane");
            csv.WriteField("Suma godzin");
            csv.WriteField("Srednia dlugosc (h)");
            csv.WriteField("Wykorzystanie (%)");
            csv.NextRecord();

            foreach (var row in report.Rows)
            {
                csv.WriteField(row.BuildingName);
                csv.WriteField(row.RoomName);
                csv.WriteField(row.RoomNumber);
                csv.WriteField(row.Capacity);
                csv.WriteField(row.ReservationCount);
                csv.WriteField(row.CancelledCount);
                csv.WriteField(row.TotalHours);
                csv.WriteField(row.AverageHours);
                csv.WriteField(row.UtilizationPercent);
                csv.NextRecord();
            }
        }
        return memory.ToArray();
    }

    public byte[] ToPdf(ReportViewModel report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(column =>
                {
                    column.Item().Text("Raport wykorzystania sal").FontSize(18).SemiBold();
                    column.Item().Text($"Okres: {report.From:dd.MM.yyyy} – {report.To:dd.MM.yyyy}");
                    column.Item().Text($"Rezerwacje: {report.TotalReservations}   |   Anulowane: {report.TotalCancelled}   |   Łącznie godzin: {report.TotalHours}");
                    column.Item().PaddingTop(8);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                    });

                    table.Header(header =>
                    {
                        void HeaderCell(string text) =>
                            header.Cell().BorderBottom(1).PaddingVertical(4).Text(text).SemiBold();

                        HeaderCell("Budynek");
                        HeaderCell("Sala");
                        HeaderCell("Numer");
                        HeaderCell("Miejsc");
                        HeaderCell("Rezerwacji");
                        HeaderCell("Anul.");
                        HeaderCell("Godzin");
                        HeaderCell("Wykorzystanie");
                    });

                    foreach (var row in report.Rows)
                    {
                        table.Cell().PaddingVertical(3).Text(row.BuildingName ?? "-");
                        table.Cell().PaddingVertical(3).Text(row.RoomName);
                        table.Cell().PaddingVertical(3).Text(row.RoomNumber);
                        table.Cell().PaddingVertical(3).Text(row.Capacity.ToString());
                        table.Cell().PaddingVertical(3).Text(row.ReservationCount.ToString());
                        table.Cell().PaddingVertical(3).Text(row.CancelledCount.ToString());
                        table.Cell().PaddingVertical(3).Text(row.TotalHours.ToString("0.#"));
                        table.Cell().PaddingVertical(3).Text($"{row.UtilizationPercent:0.#}%");
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span($"Wygenerowano {DateTime.Now:dd.MM.yyyy HH:mm}   ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
