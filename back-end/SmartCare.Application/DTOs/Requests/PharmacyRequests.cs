namespace SmartCare.Application.DTOs.Requests;

public sealed class CreatePrescriptionRequest
{
    public int PatientId { get; set; }
    public int DrugId { get; set; }
    public string Dose { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
