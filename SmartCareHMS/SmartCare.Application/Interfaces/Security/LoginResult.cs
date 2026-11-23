namespace SmartCare.Application.Interfaces.Security
{
    public sealed class LoginResult
    {
        public bool Succeeded { get; init; }

        public bool IsLockedOut { get; init; }

        public string Error { get; init; } = string.Empty;

        public AuthUser? User { get; init; }

        public static LoginResult Success(AuthUser user) => new()
        {
            Succeeded = true,
            User = user
        };

        public static LoginResult Fail(string error) => new() { Error = error };

        public static LoginResult Locked() => new()
        {
            IsLockedOut = true,
            Error = "This account is locked. Try again later."
        };
    }
}
