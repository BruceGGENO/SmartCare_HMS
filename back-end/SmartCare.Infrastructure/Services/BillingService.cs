using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SmartCare.Application.DTOs.Requests;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Infrastructure;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Infrastructure.Data;

namespace SmartCare.Infrastructure.Services;

public sealed class BillingService : IBillingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISqlExecutor _sql;
    private readonly IPaymentGateway _payments;

    public BillingService(ISqlExecutor sql, IPaymentGateway payments)
    {
        _sql = sql;
        _payments = payments;
    }

    public async Task EnsureDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _sql.QueryAsync(
            "dbo.sp_ListInvoices",
            MapInvoice,
            new Dictionary<string, object?> { ["PatientId"] = null, ["Status"] = null },
            cancellationToken);

        if (existing.Count > 0)
            return;

        var patients = await _sql.QueryAsync(
            "dbo.sp_ListPatients",
            r => new { PatientId = r.Get<int>("PatientId"), Mrn = r.Get<string>("Mrn") },
            cancellationToken: cancellationToken);

        var first = patients.FirstOrDefault();
        var second = patients.Skip(1).FirstOrDefault() ?? first;
        if (first is null)
            return;

        await CreateAsync(
            first.PatientId,
            [
                new CreateInvoiceLineRequest { Description = "General Doctor Consultation", UnitPrice = 25m, Quantity = 1 },
                new CreateInvoiceLineRequest { Description = "Complete Blood Count (CBC)", UnitPrice = 15m, Quantity = 1 }
            ],
            cancellationToken);

        if (second is not null)
        {
            await CreateAsync(
                second.PatientId,
                [
                    new CreateInvoiceLineRequest { Description = "Cardiology Consultation", UnitPrice = 45m, Quantity = 1 },
                    new CreateInvoiceLineRequest { Description = "Amoxicillin 500mg (20 capsules)", UnitPrice = 20m, Quantity = 1 }
                ],
                cancellationToken);
        }
    }

    public Task<IReadOnlyList<InvoiceResponse>> ListPendingAsync(CancellationToken cancellationToken = default) =>
        _sql.QueryAsync("dbo.sp_ListPendingInvoices", MapInvoice, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InvoiceResponse>> ListAsync(
        int? patientId,
        string? status,
        CancellationToken cancellationToken = default) =>
        _sql.QueryAsync(
            "dbo.sp_ListInvoices",
            MapInvoice,
            new Dictionary<string, object?>
            {
                ["PatientId"] = patientId,
                ["Status"] = string.IsNullOrWhiteSpace(status) ? null : status.Trim()
            },
            cancellationToken);

    public async Task<InvoiceResponse?> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_GetInvoiceById",
            MapInvoice,
            new Dictionary<string, object?> { ["InvoiceId"] = invoiceId },
            cancellationToken);

        if (rows.Count == 0)
            return null;

        var invoice = rows[0];
        invoice.Lines = await ListLinesAsync(invoiceId, cancellationToken);
        invoice.Payments = await ListPaymentsAsync(invoiceId, cancellationToken);
        return invoice;
    }

    public async Task<InvoiceResponse> CreateAsync(
        int patientId,
        IReadOnlyList<CreateInvoiceLineRequest> lines,
        CancellationToken cancellationToken = default)
    {
        var payload = lines.Select(l => new
        {
            description = l.Description.Trim(),
            unitPrice = l.UnitPrice,
            quantity = l.Quantity
        });

        try
        {
            var rows = await _sql.QueryAsync(
                "dbo.sp_CreateInvoice",
                MapInvoice,
                new Dictionary<string, object?>
                {
                    ["PatientId"] = patientId,
                    ["LinesJson"] = JsonSerializer.Serialize(payload, JsonOptions)
                },
                cancellationToken);

            var created = rows[0];
            created.Lines = await ListLinesAsync(created.InvoiceId, cancellationToken);
            return created;
        }
        catch (SqlException ex) when (ex.Number is 50001 or 50002)
        {
            throw new ArgumentException(ex.Message);
        }
    }

    public async Task<PaymentResponse> PayAsync(
        int invoiceId,
        decimal amount,
        string paymentMethod,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var charge = await _payments.ChargeAsync(amount, paymentMethod, phoneNumber, cancellationToken);
        if (!charge.IsSuccess)
            throw new InvalidOperationException(charge.Message);

        try
        {
            var rows = await _sql.QueryAsync(
                "dbo.sp_RecordPayment",
                MapPayment,
                new Dictionary<string, object?>
                {
                    ["InvoiceId"] = invoiceId,
                    ["Amount"] = amount,
                    ["PaymentMethod"] = paymentMethod.Trim(),
                    ["TransactionReference"] = charge.TransactionReference
                },
                cancellationToken);

            return rows[0];
        }
        catch (SqlException ex) when (ex.Number is 50003 or 50004 or 50005 or 50006)
        {
            throw new InvalidOperationException(ex.Message.Trim());
        }
    }

    private Task<IReadOnlyList<InvoiceLineResponse>> ListLinesAsync(int invoiceId, CancellationToken cancellationToken) =>
        _sql.QueryAsync(
            "dbo.sp_ListInvoiceLines",
            r => new InvoiceLineResponse
            {
                InvoiceLineId = r.Get<int>("InvoiceLineId"),
                InvoiceId = r.Get<int>("InvoiceId"),
                Description = r.Get<string>("Description"),
                UnitPrice = r.Get<decimal>("UnitPrice"),
                Quantity = r.Get<int>("Quantity"),
                LineTotal = r.Get<decimal>("LineTotal")
            },
            new Dictionary<string, object?> { ["InvoiceId"] = invoiceId },
            cancellationToken);

    private Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(int invoiceId, CancellationToken cancellationToken) =>
        _sql.QueryAsync(
            "dbo.sp_ListInvoicePayments",
            MapPayment,
            new Dictionary<string, object?> { ["InvoiceId"] = invoiceId },
            cancellationToken);

    private static InvoiceResponse MapInvoice(DbDataReader r) => new()
    {
        InvoiceId = r.Get<int>("InvoiceId"),
        PatientId = r.Get<int>("PatientId"),
        PatientName = r.Get<string>("PatientName"),
        PatientMrn = r.Get<string>("PatientMrn"),
        TotalAmount = r.Get<decimal>("TotalAmount"),
        PaidAmount = r.Get<decimal>("PaidAmount"),
        Status = r.Get<string>("Status"),
        CreatedAtUtc = ReadTime(r, "CreatedAtUtc")
    };

    private static PaymentResponse MapPayment(DbDataReader r) => new()
    {
        PaymentId = r.Get<int>("PaymentId"),
        InvoiceId = r.Get<int>("InvoiceId"),
        Amount = r.Get<decimal>("Amount"),
        PaymentMethod = r.Get<string>("PaymentMethod"),
        TransactionReference = r.Get<string>("TransactionReference"),
        PaidAtUtc = ReadTime(r, "PaidAtUtc")
    };

    private static DateTimeOffset ReadTime(DbDataReader record, string column)
    {
        var value = record.GetValue(record.GetOrdinal(column));
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
    }
}
