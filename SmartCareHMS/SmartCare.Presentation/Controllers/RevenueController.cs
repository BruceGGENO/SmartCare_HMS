using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Presentation.Models;

namespace SmartCare.Presentation.Controllers;

public class RevenueController : Controller
{
    private readonly IRevenueAccountingService _revenueService;

    public RevenueController(IRevenueAccountingService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _revenueService.GetSummaryAsync(cancellationToken);
        var packages = await _revenueService.GetAllPackagesAsync(cancellationToken);
        var usages = await _revenueService.GetRecentUsagesAsync(cancellationToken);

        var viewModel = new RevenueDashboardViewModel
        {
            Summary = summary,
            Packages = packages,
            RecentUsages = usages
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(PurchasePackageDto dto, CancellationToken cancellationToken)
    {
        if (dto.TotalSessions <= 0 || dto.TotalAmount <= 0 || string.IsNullOrWhiteSpace(dto.PatientName))
        {
            TempData["ErrorMessage"] = "Please fill in all package details with valid sessions and amount.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _revenueService.PurchasePackageAsync(dto, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Package '{dto.PackageName}' for {dto.PatientName} recorded successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to record package purchase.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConsumeSession(int packageId, string? notes, CancellationToken cancellationToken)
    {
        var success = await _revenueService.ConsumeSessionAsync(packageId, notes, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Session consumed successfully! Recognized revenue has been updated.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not consume session. The package may have no remaining sessions.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var bytes = await _revenueService.ExportReconciliationCsvAsync(cancellationToken);
        var filename = $"Revenue_Reconciliation_Ledger_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv", filename);
    }
}
