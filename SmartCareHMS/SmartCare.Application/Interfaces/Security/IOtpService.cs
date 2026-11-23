namespace SmartCare.Application.Interfaces.Security
{
    public interface IOtpService
    {
        Task<string?> RequestLoginOtpAsync(string userName, CancellationToken cancellationToken = default);

        Task<LoginResult> LoginWithOtpAsync(string userName, string code, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OutboxMessage>> ListRecentAsync(CancellationToken cancellationToken = default);
    }
}
