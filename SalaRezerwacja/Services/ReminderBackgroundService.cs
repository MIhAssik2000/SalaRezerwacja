using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;

namespace SalaRezerwacja.Services;

/// <summary>
/// Usługa działająca w tle: co 15 minut szuka rezerwacji zaczynających się
/// w ciągu najbliższych 24 godzin i wysyła przypomnienie (wymaganie 3).
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RemindBefore = TimeSpan.FromHours(24);

    public ReminderBackgroundService(IServiceProvider services, ILogger<ReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var limit = DateTime.Now + RemindBefore;

                var upcoming = await db.Reservations
                    .Include(r => r.Room).ThenInclude(r => r.Building)
                    .Include(r => r.User)
                    .Where(r => !r.ReminderSent
                                && r.Status == ReservationStatus.Potwierdzona
                                && r.StartTime > DateTime.Now
                                && r.StartTime <= limit)
                    .ToListAsync(stoppingToken);

                foreach (var reservation in upcoming)
                {
                    await notifications.SendUpcomingReminderAsync(reservation);
                    reservation.ReminderSent = true;
                }

                if (upcoming.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania przypomnień.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
