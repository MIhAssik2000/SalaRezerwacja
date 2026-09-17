using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;

namespace SalaRezerwacja.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.RoomCount = await _db.Rooms.CountAsync();
        ViewBag.FreeRoomCount = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Wolna);
        ViewBag.TodayReservations = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.Potwierdzona
                             && r.StartTime >= DateTime.Today
                             && r.StartTime < DateTime.Today.AddDays(1));
        return View();
    }

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult Error() => View();
}
