namespace SalaRezerwacja.Models;

/// <summary>Dostępność sali (wymaganie 2).</summary>
public enum RoomStatus
{
    Wolna = 0,
    Zajeta = 1,
    Konserwacja = 2
}

/// <summary>Typ sali – używany przy filtrowaniu (wymagania 4 i 5).</summary>
public enum RoomType
{
    Wykladowa = 0,
    Seminaryjna = 1,
    Laboratorium = 2,
    Komputerowa = 3,
    Sportowa = 4
}

public enum ReservationStatus
{
    Potwierdzona = 0,
    Anulowana = 1
}

/// <summary>Skąd pochodzi wpis w kalendarzu: od użytkownika czy z planu zajęć (wymaganie 4).</summary>
public enum ReservationSource
{
    Uzytkownik = 0,
    PlanZajec = 1
}
