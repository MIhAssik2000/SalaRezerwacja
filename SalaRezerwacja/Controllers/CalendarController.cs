using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;
using SalaRezerwacja.Models.ViewModels;
using SalaRezerwacja.Services;

namespace SalaRezerwacja.Controllers;

/// <summary>Kalendarz zajętości sal i synchronizacja z planem zajęć (wymaganie 4).</summary>
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IReservationService _reservations;
    private readonly ScheduleImportService _scheduleImport;
    private readonly UserManager<ApplicationUser> _userManager;

    public CalendarController(
        ApplicationDbContext db,
        IReservationService reservations,
        ScheduleImportService scheduleImport,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _reservations = reservations;
        _scheduleImport = scheduleImport;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(RoomSearchViewModel filter)
    {
        filter ??= new RoomSearchViewModel();
        filter.Buildings = await _db.Buildings.OrderBy(b => b.Name).ToListAsync();
        filter.AllEquipment = await _db.Equipment.OrderBy(e => e.Name).ToListAsync();
        return View(filter);
    }

    /// <summary>Źródło danych dla FullCalendar – zwraca wydarzenia w formacie JSON.</summary>
    [HttpGet]
    public async Task<IActionResult> Events(DateTime start, DateTime end, int? buildingId, RoomType? type, int? minCapacity, [FromQuery] List<int> equipmentIds)
    {
        var filter = new RoomSearchViewModel
        {
            BuildingId = buildingId,
            Type = type,
            MinCapacity = minCapacity,
            EquipmentIds = equipmentIds ?? new List<int>()
        };

        var reservations = await _reservations.GetCalendarAsync(start, end, filter);

        var events = reservations.Select(r => new
        {
            id = r.Id,
            title = $"{r.Room.Number} – {r.Title}",
            start = r.StartTime.ToString("s"),
            end = r.EndTime.ToString("s"),
            // Zajęcia z planu uczelni odróżniamy kolorem od zwykłych rezerwacji.
            color = r.Source == ReservationSource.PlanZajec ? "#6b7280" : "#1d4ed8",
            extendedProps = new
            {
                room = r.Room.FullLabel,
                building = r.Room.Building?.Name,
                user = r.User?.FullName,
                source = r.Source.ToString()
            }
        });

        return Json(events);
    }

    [Authorize(Roles = Roles.Administracja)]
    public IActionResult Import() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administracja)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Wybierz plik .ics z planem zajęć.";
            return View();
        }

        using var stream = file.OpenReadStream();
        var (imported, skipped, problems) = await _scheduleImport.ImportIcsAsync(stream, _userManager.GetUserId(User));

        TempData["Success"] = $"Zaimportowano {imported} zajęć, pominięto {skipped}.";
        ViewBag.Problems = problems;
        return View();
    }
}
