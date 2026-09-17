using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;
using SalaRezerwacja.Models.ViewModels;
using SalaRezerwacja.Services;

namespace SalaRezerwacja.Controllers;

/// <summary>Rezerwacja sal (wymaganie 3).</summary>
[Authorize]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IReservationService _reservations;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReservationsController(ApplicationDbContext db, IReservationService reservations, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _reservations = reservations;
        _userManager = userManager;
    }

    /// <summary>Moje rezerwacje; administracja widzi wszystkie.</summary>
    public async Task<IActionResult> Index(bool all = false)
    {
        var userId = _userManager.GetUserId(User);
        var showAll = all && User.IsInRole(Roles.Administracja);

        var query = _db.Reservations
            .Include(r => r.Room).ThenInclude(r => r.Building)
            .Include(r => r.User)
            .AsQueryable();

        if (!showAll)
            query = query.Where(r => r.UserId == userId);

        ViewBag.ShowAll = showAll;
        var list = await query.OrderByDescending(r => r.StartTime).Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create(int? roomId, DateTime? date, TimeSpan? from, TimeSpan? to)
    {
        var model = new ReservationCreateViewModel
        {
            RoomId = roomId ?? 0,
            Date = date ?? DateTime.Today,
            FromTime = from ?? new TimeSpan(8, 0, 0),
            ToTime = to ?? new TimeSpan(9, 30, 0),
            Rooms = await AvailableRooms()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Rooms = await AvailableRooms();
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var result = await _reservations.CreateAsync(
            model.RoomId, userId, model.Title, model.StartDateTime, model.EndDateTime);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.Rooms = await AvailableRooms();
            return View(model);
        }

        TempData["Success"] = "Rezerwacja potwierdzona. Potwierdzenie wysłano na adres e-mail.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User);
        var result = await _reservations.CancelAsync(id, userId, User.IsInRole(Roles.Administracja));

        if (result.Success)
            TempData["Success"] = "Rezerwacja została anulowana.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Podpowiedź dla formularza: limit czasu wybranej sali.</summary>
    [HttpGet]
    public async Task<IActionResult> RoomLimit(int roomId)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return NotFound();
        return Json(new { maxMinutes = room.MaxReservationMinutes, status = room.Status.ToString() });
    }

    private async Task<List<Room>> AvailableRooms() =>
        await _db.Rooms
            .Include(r => r.Building)
            .Where(r => r.Status != RoomStatus.Konserwacja)
            .OrderBy(r => r.Building.Name).ThenBy(r => r.Number)
            .ToListAsync();
}
