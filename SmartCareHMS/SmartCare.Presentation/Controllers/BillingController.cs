using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.Interfaces.Services;

namespace SmartCare.Presentation.Controllers;

public class BillingController : Controller
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    // GET: /Billing
    public async Task<IActionResult> Index()
    {
        var invoices = await _billingService.GetPendingInvoicesAsync();
        return View(invoices);
    }

    // GET: /Billing/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var invoice = await _billingService.GetInvoiceByIdAsync(id);
        if (invoice is null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    // POST: /Billing/Pay
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int invoiceId, decimal amount, string paymentMethod, string phoneNumber)
    {
        if (amount <= 0)
        {
            TempData["Error"] = "Payment amount must be greater than zero.";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            TempData["Error"] = "Please enter a valid Mobile Money phone number.";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        var success = await _billingService.PayInvoiceAsync(invoiceId, amount, paymentMethod, phoneNumber);

        if (success)
        {
            TempData["Success"] = $"Payment prompt confirmed on {phoneNumber}! Amount: {amount:C} via {paymentMethod}.";
        }
        else
        {
            TempData["Error"] = "Payment processing failed. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { id = invoiceId });
    }
}
