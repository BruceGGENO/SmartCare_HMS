using SmartCare.Application.DTOs;

namespace SmartCare.Application.Interfaces.Services;

public interface IBillingService
{
    Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceDto>> GetPendingInvoicesAsync(CancellationToken cancellationToken = default);

    Task<bool> PayInvoiceAsync(
        int invoiceId, 
        decimal amount, 
        string paymentMethod, 
        string phoneNumber = "", 
        CancellationToken cancellationToken = default);
}
