using SalaRezerwacja.Models;

namespace SalaRezerwacja.Services;

/// <summary>Dane użytkownika zwrócone przez uczelniany system tożsamości.</summary>
public record UniversityUserInfo(string Email, string FirstName, string LastName, string UniversityId, string Role);

public interface IUniversityAuthProvider
{
    bool IsConfigured { get; }
    string ProviderName { get; }

    /// <summary>Weryfikuje dane w uczelnianym systemie i zwraca profil użytkownika.</summary>
    Task<UniversityUserInfo> AuthenticateAsync(string login, string password);
}

/// <summary>
/// WYMAGANIE 1 – logowanie kontem uczelnianym.
///
/// W środowisku produkcyjnym uczelnia udostępnia jeden z mechanizmów:
///   • Microsoft Entra ID (dawniej Azure AD) – konta Office 365, protokół OpenID Connect.
///     Pakiet: Microsoft.Identity.Web, rejestracja aplikacji w Azure Portal,
///     konfiguracja: Authority / ClientId / ClientSecret / RedirectUri.
///   • CAS (Central Authentication Service) – popularny na polskich uczelniach.
///   • LDAP / Active Directory – bezpośrednie sprawdzanie poświadczeń w katalogu.
///   • USOS API – gdy uczelnia korzysta z USOS-a (OAuth 1.0a).
///
/// Do podłączenia któregokolwiek z nich potrzebne są dane rejestracyjne aplikacji,
/// wydawane przez dział IT uczelni. Dlatego w projekcie zaimplementowano
/// interfejs oraz działającą atrapę dla środowiska deweloperskiego: wystarczy
/// dopisać drugą klasę implementującą IUniversityAuthProvider i zarejestrować ją
/// w Program.cs, bez zmian w reszcie systemu.
/// </summary>
public class DevUniversityAuthProvider : IUniversityAuthProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<DevUniversityAuthProvider> _logger;

    public DevUniversityAuthProvider(IConfiguration config, ILogger<DevUniversityAuthProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured => _config.GetValue<bool>("UniversitySso:Enabled");

    public string ProviderName => "Konto uczelniane (symulacja)";

    private string Domain => _config.GetValue<string>("UniversitySso:EmailDomain") ?? "uczelnia.edu.pl";

    public Task<UniversityUserInfo> AuthenticateAsync(string login, string password)
    {
        // Atrapa: akceptuje dowolny login w domenie uczelni z hasłem dłuższym niż 5 znaków.
        // Prawdziwa implementacja odpytywałaby tutaj Entra ID / CAS / LDAP.
        if (string.IsNullOrWhiteSpace(login) || !login.EndsWith("@" + Domain, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<UniversityUserInfo>(null);

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return Task.FromResult<UniversityUserInfo>(null);

        var localPart = login.Split('@')[0];
        var parts = localPart.Split('.', 2);
        var first = Capitalize(parts.ElementAtOrDefault(0) ?? "Użytkownik");
        var last = Capitalize(parts.ElementAtOrDefault(1) ?? "Uczelniany");

        _logger.LogInformation("Symulowane logowanie uczelniane dla {Login}", login);

        return Task.FromResult(new UniversityUserInfo(
            Email: login,
            FirstName: first,
            LastName: last,
            UniversityId: Math.Abs(login.GetHashCode()).ToString()[..6],
            Role: Roles.Student));
    }

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0]) + value[1..];
}
