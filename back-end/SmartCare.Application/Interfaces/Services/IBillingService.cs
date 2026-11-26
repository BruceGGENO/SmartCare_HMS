using SmartCare.Application.DTOs.Requests;
using SmartCare.Application.DTOs.Responses;

namespace SmartCare.Application.Interfaces.Services;

public interface IBillingService
{
    Task EnsureDemoDataAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceResponse>> ListPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceResponse>> ListAsync(
        int? patientId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse?> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default);

    Task<InvoiceResponse> CreateAsync(
        int patientId,
        IReadOnlyList<CreateInvoiceLineRequest> lines,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> PayAsync(
        int invoiceId,
        decimal amount,
        string paymentMethod,
        string? phoneNumber,
        CancellationToken cancellationToken = default);
}
