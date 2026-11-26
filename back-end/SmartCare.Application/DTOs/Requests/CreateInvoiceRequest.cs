namespace SmartCare.Application.DTOs.Requests;

public sealed class CreateInvoiceRequest
{
    public int PatientId { get; set; }

    public List<CreateInvoiceLineRequest> Lines { get; set; } = [];
}

public sealed class CreateInvoiceLineRequest
{
    public string Description { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;
}
