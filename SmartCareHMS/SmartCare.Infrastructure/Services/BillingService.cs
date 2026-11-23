using SmartCare.Application.DTOs;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly ISqlExecutor _sql;
    private readonly IPaymentGateway _paymentGateway;

    public BillingService(ISqlExecutor sql, IPaymentGateway paymentGateway)
    {
        _sql = sql;
        _paymentGateway = paymentGateway;
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetPendingInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await _sql.QueryAsync(
            "dbo.sp_GetPendingInvoices",
            map: r => new InvoiceDto
            {
                Id = r.Get<int>("Id"),
                PatientId = r.Get<int>("PatientId"),
                PatientName = r.Get<string>("PatientName"),
                PatientMrn = r.Get<string>("PatientMrn"),
                TotalAmount = r.Get<decimal>("TotalAmount"),
                PaidAmount = r.Get<decimal>("PaidAmount"),
                Status = r.Get<string>("Status"),
                CreatedAt = r.Get<DateTime>("CreatedAt")
            },
            cancellationToken: cancellationToken);
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?> { ["InvoiceId"] = invoiceId };

        var invoices = await _sql.QueryAsync(
            "dbo.sp_GetInvoiceById",
            map: r => new InvoiceDto
            {
                Id = r.Get<int>("Id"),
                PatientId = r.Get<int>("PatientId"),
                PatientName = r.Get<string>("PatientName"),
                PatientMrn = r.Get<string>("PatientMrn"),
                TotalAmount = r.Get<decimal>("TotalAmount"),
                PaidAmount = r.Get<decimal>("PaidAmount"),
                Status = r.Get<string>("Status"),
                CreatedAt = r.Get<DateTime>("CreatedAt")
            },
            parameters: parameters,
            cancellationToken: cancellationToken);

        var invoice = invoices.FirstOrDefault();
        if (invoice is null)
        {
            return null;
        }

        var lines = await _sql.QueryAsync(
            "dbo.sp_GetInvoiceLines",
            map: r => new InvoiceLineDto
            {
                Id = r.Get<int>("Id"),
                InvoiceId = r.Get<int>("InvoiceId"),
                Description = r.Get<string>("Description"),
                UnitPrice = r.Get<decimal>("UnitPrice"),
                Quantity = r.Get<int>("Quantity"),
                LineTotal = r.Get<decimal>("LineTotal")
            },
            parameters: parameters,
            cancellationToken: cancellationToken);

        invoice.Lines = lines.ToList();
        return invoice;
    }

    public async Task<bool> PayInvoiceAsync(
        int invoiceId, 
        decimal amount, 
        string paymentMethod, 
        string phoneNumber = "", 
        CancellationToken cancellationToken = default)
    {
        var chargeResult = await _paymentGateway.ChargeAsync(amount, paymentMethod, phoneNumber, cancellationToken);
        if (!chargeResult.IsSuccess)
        {
            return false;
        }

        var parameters = new Dictionary<string, object?>
        {
            ["InvoiceId"] = invoiceId,
            ["Amount"] = amount,
            ["PaymentMethod"] = paymentMethod,
            ["TransactionReference"] = chargeResult.TransactionReference
        };

        await _sql.ExecuteNonQueryAsync(
            "dbo.sp_RecordPayment",
            parameters: parameters,
            cancellationToken: cancellationToken);

        return true;
    }
}
