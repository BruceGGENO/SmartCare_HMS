namespace SmartCare.Application.Interfaces.Security;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);

    Task<AuthUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task EnsureDemoUsersAsync(CancellationToken cancellationToken = default);
}
