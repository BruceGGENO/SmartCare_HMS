namespace SmartCare.Application.Interfaces.Security;

public interface ICheckInCodeGenerator
{
    CheckInCode Create(string token);
}
