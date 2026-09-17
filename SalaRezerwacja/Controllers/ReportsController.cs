using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalaRezerwacja.Models;
using SalaRezerwacja.Services;

namespace SalaRezerwacja.Controllers;

/// <summary>Raporty, statystyki i eksport danych (wymaganie 6).</summary>
[Authorize(Roles = Roles.Administracja + "," + Roles.Opiekun)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? buildingId)
    {
        var model = await _reports.BuildAsync(
            from ?? DateTime.Today.AddMonths(-1),
            to ?? DateTime.Today.AddDays(1),
            buildingId);
        return View(model);
    }

    public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to, int? buildingId)
    {
        var model = await _reports.BuildAsync(from ?? DateTime.Today.AddMonths(-1), to ?? DateTime.Today.AddDays(1), buildingId);
        var bytes = _reports.ToCsv(model);
        return File(bytes, "text/csv", $"raport-sal-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to, int? buildingId)
    {
        var model = await _reports.BuildAsync(from ?? DateTime.Today.AddMonths(-1), to ?? DateTime.Today.AddDays(1), buildingId);
        var bytes = _reports.ToPdf(model);
        return File(bytes, "application/pdf", $"raport-sal-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }
}
