using MailKit.Net.Smtp;
using MimeKit;
using SalaRezerwacja.Models;

namespace SalaRezerwacja.Services;

public interface INotificationService
{
    Task SendReservationConfirmedAsync(Reservation reservation);
    Task SendReservationCancelledAsync(Reservation reservation);
    Task SendUpcomingReminderAsync(Reservation reservation);
}

public class SmtpOptions
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string From { get; set; }
    public bool Enabled { get; set; }
}

public class SmsOptions
{
    public string Provider { get; set; }
    public string AccountSid { get; set; }
    public string AuthToken { get; set; }
    public string FromNumber { get; set; }
    public bool Enabled { get; set; }
}

/// <summary>
/// Powiadomienia e-mail i SMS (wymaganie 3).
/// Gdy w appsettings.json wyłączone (Enabled=false), treść trafia tylko do logów –
/// dzięki temu aplikacja działa na uczelnianym laptopie bez serwera pocztowego.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly SmtpOptions _smtp;
    private readonly SmsOptions _sms;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IConfiguration config, ILogger<NotificationService> logger)
    {
        _smtp = config.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
        _sms = config.GetSection("Sms").Get<SmsOptions>() ?? new SmsOptions();
        _logger = logger;
    }

    public Task SendReservationConfirmedAsync(Reservation r) =>
        Send(r, "Potwierdzenie rezerwacji sali",
            $"Rezerwacja została potwierdzona.\n\n{Describe(r)}\n\nRezerwację można anulować do momentu jej rozpoczęcia.");

    public Task SendReservationCancelledAsync(Reservation r) =>
        Send(r, "Anulowanie rezerwacji sali",
            $"Rezerwacja została anulowana.\n\n{Describe(r)}");

    public Task SendUpcomingReminderAsync(Reservation r) =>
        Send(r, "Przypomnienie o nadchodzącej rezerwacji",
            $"Przypominamy o rezerwacji, która rozpocznie się wkrótce.\n\n{Describe(r)}");

    private static string Describe(Reservation r) =>
        $"Sala: {r.Room?.Name} ({r.Room?.Number})\n" +
        $"Budynek: {r.Room?.Building?.Name}, piętro {r.Room?.Floor}\n" +
        $"Termin: {r.StartTime:dd.MM.yyyy HH:mm} – {r.EndTime:HH:mm}\n" +
        $"Cel: {r.Title}";

    private async Task Send(Reservation reservation, string subject, string body)
    {
        var email = reservation.User?.Email;
        var phone = reservation.User?.PhoneNumber;

        if (_smtp.Enabled && !string.IsNullOrWhiteSpace(email))
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_smtp.From));
                message.To.Add(MailboxAddress.Parse(email));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtp.Host, _smtp.Port, MailKit.Security.SecureSocketOptions.Auto);
                if (!string.IsNullOrWhiteSpace(_smtp.User))
                    await client.AuthenticateAsync(_smtp.User, _smtp.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Błąd wysyłki nie może wywalić rezerwacji – logujemy i idziemy dalej.
                _logger.LogError(ex, "Nie udało się wysłać e-maila do {Email}", email);
            }
        }
        else
        {
            _logger.LogInformation("[E-MAIL -> {Email}] {Subject}\n{Body}", email, subject, body);
        }

        if (_sms.Enabled && !string.IsNullOrWhiteSpace(phone))
        {
            // Miejsce na integrację z bramką SMS (np. Twilio):
            // var client = new TwilioRestClient(_sms.AccountSid, _sms.AuthToken);
            // await MessageResource.CreateAsync(to: phone, from: _sms.FromNumber, body: body);
            _logger.LogInformation("[SMS -> {Phone}] {Subject}", phone, subject);
        }
        else
        {
            _logger.LogInformation("[SMS -> {Phone}] {Subject} (wysyłka wyłączona)", phone, subject);
        }
    }
}
