using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;

namespace SalaRezerwacja.Services;

/// <summary>
/// WYMAGANIE 4 – synchronizacja z planem zajęć uczelni.
///
/// Uczelniane plany zajęć są zwykle udostępniane jako pliki iCalendar (.ics)
/// eksportowane z USOS-a / Planu zajęć. Serwis parsuje taki plik i zapisuje
/// zajęcia jako rezerwacje o źródle PlanZajec – widać je w kalendarzu
/// i blokują one możliwość rezerwacji sali przez użytkowników.
///
/// Dopasowanie sali odbywa się po polu LOCATION, które porównujemy
/// z numerem sali w bazie.
/// </summary>
public class ScheduleImportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ScheduleImportService> _logger;

    public ScheduleImportService(ApplicationDbContext db, ILogger<ScheduleImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(int imported, int skipped, List<string> problems)> ImportIcsAsync(Stream icsStream, string systemUserId)
    {
        var problems = new List<string>();
        int imported = 0, skipped = 0;

        using var reader = new StreamReader(icsStream);
        var content = await reader.ReadToEndAsync();

        var rooms = await _db.Rooms.ToListAsync();

        foreach (var rawEvent in SplitEvents(content))
        {
            var summary = GetValue(rawEvent, "SUMMARY");
            var location = GetValue(rawEvent, "LOCATION");
            var startText = GetValue(rawEvent, "DTSTART");
            var endText = GetValue(rawEvent, "DTEND");

            if (!TryParseIcsDate(startText, out var start) || !TryParseIcsDate(endText, out var end))
            {
                problems.Add($"Pominięto wydarzenie „{summary}” – nieczytelna data.");
                skipped++;
                continue;
            }

            var room = rooms.FirstOrDefault(r =>
                !string.IsNullOrWhiteSpace(location) &&
                (location.Contains(r.Number, StringComparison.OrdinalIgnoreCase) ||
                 location.Contains(r.Name, StringComparison.OrdinalIgnoreCase)));

            if (room == null)
            {
                problems.Add($"Pominięto „{summary}” – nie rozpoznano sali „{location}”.");
                skipped++;
                continue;
            }

            // Nie duplikujemy zajęć przy ponownym imporcie tego samego pliku.
            var exists = await _db.Reservations.AnyAsync(r =>
                r.RoomId == room.Id && r.StartTime == start && r.EndTime == end &&
                r.Source == ReservationSource.PlanZajec);

            if (exists) { skipped++; continue; }

            _db.Reservations.Add(new Reservation
            {
                RoomId = room.Id,
                UserId = systemUserId,
                Title = string.IsNullOrWhiteSpace(summary) ? "Zajęcia dydaktyczne" : summary,
                StartTime = start,
                EndTime = end,
                Status = ReservationStatus.Potwierdzona,
                Source = ReservationSource.PlanZajec,
                ReminderSent = true // przypomnienia dotyczą tylko rezerwacji użytkowników
            });
            imported++;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Import planu zajęć: {Imported} dodanych, {Skipped} pominiętych.", imported, skipped);

        return (imported, skipped, problems);
    }

    private static IEnumerable<string> SplitEvents(string content)
    {
        var parts = content.Split("BEGIN:VEVENT", StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < parts.Length; i++)
        {
            var end = parts[i].IndexOf("END:VEVENT", StringComparison.Ordinal);
            yield return end >= 0 ? parts[i][..end] : parts[i];
        }
    }

    private static string GetValue(string block, string key)
    {
        foreach (var line in block.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(key + ";", StringComparison.OrdinalIgnoreCase))
            {
                var colon = trimmed.IndexOf(':');
                if (colon >= 0) return trimmed[(colon + 1)..].Trim();
            }
        }
        return null;
    }

    private static bool TryParseIcsDate(string value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var formats = new[] { "yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmmss", "yyyyMMdd" };
        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out result))
            return true;

        return DateTime.TryParse(value, out result);
    }
}
