using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SalaRezerwacja.Models;

/// <summary>
/// Rozszerzenie standardowego użytkownika Identity o pola uczelniane.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Display(Name = "Imię")]
    [MaxLength(60)]
    public string FirstName { get; set; }

    [Display(Name = "Nazwisko")]
    [MaxLength(80)]
    public string LastName { get; set; }

    /// <summary>Numer albumu / numer pracownika.</summary>
    [Display(Name = "Numer uczelniany")]
    [MaxLength(30)]
    public string UniversityId { get; set; }

    /// <summary>True, jeżeli konto powstało przez logowanie kontem uczelnianym (SSO).</summary>
    public bool IsUniversityAccount { get; set; }

    public string FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Email
        : $"{FirstName} {LastName}".Trim();
}

/// <summary>Nazwy ról – w jednym miejscu, żeby uniknąć literówek.</summary>
public static class Roles
{
    public const string Student = "Student";
    public const string Opiekun = "Opiekun";
    public const string Administracja = "Administracja";
}
