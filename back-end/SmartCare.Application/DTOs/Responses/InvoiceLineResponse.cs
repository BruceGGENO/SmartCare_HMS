namespace SmartCare.Application.DTOs.Responses;

public sealed class InvoiceLineResponse
{
    public int InvoiceLineId { get; set; }

    public int InvoiceId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
