namespace SmartCare.Application.DTOs.Requests;

public sealed class AdmitPatientRequest
{
    public int PatientId { get; set; }
    public int BedId { get; set; }
}

public sealed class TransferAdmissionRequest
{
    public int NewBedId { get; set; }
}
