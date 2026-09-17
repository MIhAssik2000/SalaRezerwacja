using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;
using SalaRezerwacja.Models.ViewModels;

namespace SalaRezerwacja.Services;

public record OperationResult(bool Success, string Error = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string error) => new(false, error);
}

public interface IReservationService
{
    Task<bool> IsRoomFreeAsync(int roomId, DateTime start, DateTime end, int? ignoreReservationId = null);
    Task<OperationResult> CreateAsync(int roomId, string userId, string title, DateTime start, DateTime end);
    Task<OperationResult> CancelAsync(int reservationId, string userId, bool isAdmin);
    Task<List<Room>> SearchAsync(RoomSearchViewModel filter);
    Task<List<Reservation>> GetCalendarAsync(DateTime from, DateTime to, RoomSearchViewModel filter);
}

/// <summary>
/// Cała logika biznesowa rezerwacji. Kontrolery tylko ją wołają –
/// dzięki temu reguły są w jednym miejscu i dają się testować.
/// </summary>
public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public ReservationService(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <summary>
    /// Sprawdzenie kolizji terminów. Dwa okresy nachodzą na siebie wtedy i tylko wtedy,
    /// gdy: nowyPoczątek &lt; istniejącyKoniec ORAZ nowyKoniec &gt; istniejącyPoczątek.
    /// </summary>
    public async Task<bool> IsRoomFreeAsync(int roomId, DateTime start, DateTime end, int? ignoreReservationId = null)
    {
        return !await _db.Reservations.AnyAsync(r =>
            r.RoomId == roomId &&
            r.Status == ReservationStatus.Potwierdzona &&
            (ignoreReservationId == null || r.Id != ignoreReservationId) &&
            start < r.EndTime &&
            end > r.StartTime);
    }

    public async Task<OperationResult> CreateAsync(int roomId, string userId, string title, DateTime start, DateTime end)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null)
            return OperationResult.Fail("Wybrana sala nie istnieje.");

        if (room.Status == RoomStatus.Konserwacja)
            return OperationResult.Fail("Sala jest w konserwacji i nie można jej rezerwować.");

        if (end <= start)
            return OperationResult.Fail("Godzina zakończenia musi być późniejsza niż rozpoczęcia.");

        if (start < DateTime.Now)
            return OperationResult.Fail("Nie można rezerwować terminu w przeszłości.");

        // Wymaganie 3: limit długości rezerwacji zależny od sali.
        var minutes = (end - start).TotalMinutes;
        if (minutes > room.MaxReservationMinutes)
            return OperationResult.Fail(
                $"Maksymalny czas rezerwacji tej sali to {room.MaxReservationMinutes} minut " +
                $"({room.MaxReservationMinutes / 60.0:0.#} h). Wybrany termin ma {minutes:0} minut.");

        if (!await IsRoomFreeAsync(roomId, start, end))
            return OperationResult.Fail("Sala jest już zarezerwowana w tym terminie.");

        var reservation = new Reservation
        {
            RoomId = roomId,
            UserId = userId,
            Title = title,
            StartTime = start,
            EndTime = end,
            Status = ReservationStatus.Potwierdzona,
            Source = ReservationSource.Uzytkownik,
            CreatedAt = DateTime.Now
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        // Doczytujemy powiązania, żeby powiadomienie miało komplet danych.
        await _db.Entry(reservation).Reference(r => r.Room).LoadAsync();
        await _db.Entry(reservation.Room).Reference(r => r.Building).LoadAsync();
        await _db.Entry(reservation).Reference(r => r.User).LoadAsync();

        await _notifications.SendReservationConfirmedAsync(reservation);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> CancelAsync(int reservationId, string userId, bool isAdmin)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Room).ThenInclude(r => r.Building)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            return OperationResult.Fail("Rezerwacja nie istnieje.");

        if (!isAdmin && reservation.UserId != userId)
            return OperationResult.Fail("Możesz anulować tylko własne rezerwacje.");

        if (reservation.Status == ReservationStatus.Anulowana)
            return OperationResult.Fail("Ta rezerwacja została już anulowana.");

        // Wymaganie 3: anulowanie tylko przed rozpoczęciem obowiązywania.
        if (reservation.StartTime <= DateTime.Now)
            return OperationResult.Fail("Rezerwacja już się rozpoczęła – nie można jej anulować.");

        reservation.Status = ReservationStatus.Anulowana;
        reservation.CancelledAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await _notifications.SendReservationCancelledAsync(reservation);
        return OperationResult.Ok();
    }

    /// <summary>Wyszukiwanie i filtrowanie sal (wymagania 4 i 5).</summary>
    public async Task<List<Room>> SearchAsync(RoomSearchViewModel filter)
    {
        var query = _db.Rooms
            .Include(r => r.Building)
            .Include(r => r.RoomEquipments).ThenInclude(re => re.Equipment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
            query = query.Where(r => r.Name.Contains(filter.Query) || r.Number.Contains(filter.Query));

        if (filter.BuildingId.HasValue)
            query = query.Where(r => r.BuildingId == filter.BuildingId);

        if (filter.Floor.HasValue)
            query = query.Where(r => r.Floor == filter.Floor);

        if (filter.MinCapacity.HasValue)
            query = query.Where(r => r.Capacity >= filter.MinCapacity);

        if (filter.Type.HasValue)
            query = query.Where(r => r.Type == filter.Type);

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status);

        // Sala musi mieć KAŻDY z zaznaczonych elementów wyposażenia.
        foreach (var equipmentId in filter.EquipmentIds ?? new List<int>())
        {
            var id = equipmentId;
            query = query.Where(r => r.RoomEquipments.Any(re => re.EquipmentId == id));
        }

        var rooms = await query.OrderBy(r => r.Building.Name).ThenBy(r => r.Number).ToListAsync();

        // Jeżeli podano przedział czasowy – zostawiamy tylko sale faktycznie wolne.
        if (filter.HasTimeRange)
        {
            var start = filter.StartDateTime.Value;
            var end = filter.EndDateTime.Value;

            var busyRoomIds = await _db.Reservations
                .Where(r => r.Status == ReservationStatus.Potwierdzona && start < r.EndTime && end > r.StartTime)
                .Select(r => r.RoomId)
                .Distinct()
                .ToListAsync();

            rooms = rooms
                .Where(r => !busyRoomIds.Contains(r.Id) && r.Status != RoomStatus.Konserwacja)
                .ToList();
        }

        return rooms;
    }

    /// <summary>Dane do kalendarza zajętości (wymaganie 4).</summary>
    public async Task<List<Reservation>> GetCalendarAsync(DateTime from, DateTime to, RoomSearchViewModel filter)
    {
        var query = _db.Reservations
            .Include(r => r.Room).ThenInclude(r => r.Building)
            .Include(r => r.User)
            .Where(r => r.Status == ReservationStatus.Potwierdzona && r.StartTime < to && r.EndTime > from);

        if (filter?.BuildingId != null)
            query = query.Where(r => r.Room.BuildingId == filter.BuildingId);

        if (filter?.Type != null)
            query = query.Where(r => r.Room.Type == filter.Type);

        if (filter?.MinCapacity != null)
            query = query.Where(r => r.Room.Capacity >= filter.MinCapacity);

        foreach (var equipmentId in filter?.EquipmentIds ?? new List<int>())
        {
            var id = equipmentId;
            query = query.Where(r => r.Room.RoomEquipments.Any(re => re.EquipmentId == id));
        }

        return await query.ToListAsync();
    }
}
