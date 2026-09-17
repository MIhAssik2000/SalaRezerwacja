using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;
using SalaRezerwacja.Models.ViewModels;
using SalaRezerwacja.Services;

namespace SalaRezerwacja.Controllers;

/// <summary>
/// Zarządzanie salami (wymaganie 2) oraz wyszukiwanie i filtrowanie (wymaganie 5).
/// </summary>
[Authorize]
public class RoomsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IReservationService _reservations;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomsController(ApplicationDbContext db, IReservationService reservations, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _reservations = reservations;
        _userManager = userManager;
    }

    /// <summary>Lista sal z pełnym zestawem filtrów.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> Index(RoomSearchViewModel filter)
    {
        filter ??= new RoomSearchViewModel();
        filter.Results = await _reservations.SearchAsync(filter);
        await FillDictionaries(filter);
        return View(filter);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.Building)
            .Include(r => r.Opiekun)
            .Include(r => r.RoomEquipments).ThenInclude(re => re.Equipment)
            .Include(r => r.Reservations.Where(res => res.EndTime >= DateTime.Now && res.Status == ReservationStatus.Potwierdzona))
                .ThenInclude(res => res.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null) return NotFound();
        return View(room);
    }

    [Authorize(Roles = Roles.Opiekun + "," + Roles.Administracja)]
    public async Task<IActionResult> Create()
    {
        var model = new RoomFormViewModel();
        await FillDictionaries(model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Opiekun + "," + Roles.Administracja)]
    public async Task<IActionResult> Create(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillDictionaries(model);
            return View("Form", model);
        }

        if (await _db.Rooms.AnyAsync(r => r.Number == model.Number && r.BuildingId == model.BuildingId))
        {
            ModelState.AddModelError(nameof(model.Number), "Sala o tym numerze już istnieje w wybranym budynku.");
            await FillDictionaries(model);
            return View("Form", model);
        }

        var room = new Room();
        Apply(model, room);
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        await SaveEquipment(room.Id, model.EquipmentIds);

        TempData["Success"] = "Sala została dodana.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Opiekun + "," + Roles.Administracja)]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomEquipments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null) return NotFound();
        if (!await CanEdit(room)) return Forbid();

        var model = new RoomFormViewModel
        {
            Id = room.Id,
            Name = room.Name,
            Number = room.Number,
            BuildingId = room.BuildingId,
            Floor = room.Floor,
            Capacity = room.Capacity,
            Type = room.Type,
            Status = room.Status,
            MaxReservationMinutes = room.MaxReservationMinutes,
            OpiekunId = room.OpiekunId,
            Description = room.Description,
            EquipmentIds = room.RoomEquipments.Select(re => re.EquipmentId).ToList()
        };

        await FillDictionaries(model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Opiekun + "," + Roles.Administracja)]
    public async Task<IActionResult> Edit(RoomFormViewModel model)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == model.Id);
        if (room == null) return NotFound();
        if (!await CanEdit(room)) return Forbid();

        if (!ModelState.IsValid)
        {
            await FillDictionaries(model);
            return View("Form", model);
        }

        Apply(model, room);
        await _db.SaveChangesAsync();
        await SaveEquipment(room.Id, model.EquipmentIds);

        TempData["Success"] = "Zmiany zostały zapisane.";
        return RedirectToAction(nameof(Details), new { id = room.Id });
    }

    // ---------- pomocnicze ----------

    private static void Apply(RoomFormViewModel model, Room room)
    {
        room.Name = model.Name;
        room.Number = model.Number;
        room.BuildingId = model.BuildingId;
        room.Floor = model.Floor;
        room.Capacity = model.Capacity;
        room.Type = model.Type;
        room.Status = model.Status;
        room.MaxReservationMinutes = model.MaxReservationMinutes;
        room.OpiekunId = string.IsNullOrWhiteSpace(model.OpiekunId) ? null : model.OpiekunId;
        room.Description = model.Description;
    }

    private async Task SaveEquipment(int roomId, List<int> equipmentIds)
    {
        var existing = await _db.RoomEquipments.Where(re => re.RoomId == roomId).ToListAsync();
        _db.RoomEquipments.RemoveRange(existing);

        foreach (var equipmentId in equipmentIds ?? new List<int>())
            _db.RoomEquipments.Add(new RoomEquipment { RoomId = roomId, EquipmentId = equipmentId });

        await _db.SaveChangesAsync();
    }

    /// <summary>Opiekun edytuje tylko swoje sale, administracja – wszystkie.</summary>
    private async Task<bool> CanEdit(Room room)
    {
        if (User.IsInRole(Roles.Administracja)) return true;
        var userId = _userManager.GetUserId(User);
        return room.OpiekunId == null || room.OpiekunId == userId;
    }

    private async Task FillDictionaries(RoomSearchViewModel model)
    {
        model.Buildings = await _db.Buildings.OrderBy(b => b.Name).ToListAsync();
        model.AllEquipment = await _db.Equipment.OrderBy(e => e.Name).ToListAsync();
    }

    private async Task FillDictionaries(RoomFormViewModel model)
    {
        model.Buildings = await _db.Buildings.OrderBy(b => b.Name).ToListAsync();
        model.AllEquipment = await _db.Equipment.OrderBy(e => e.Name).ToListAsync();
        var opiekunowie = await _userManager.GetUsersInRoleAsync(Roles.Opiekun);
        model.Opiekunowie = opiekunowie.OrderBy(u => u.LastName).ToList();
    }
}
