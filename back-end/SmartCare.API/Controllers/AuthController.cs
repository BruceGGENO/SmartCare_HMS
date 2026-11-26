using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCare.Application.DTOs.Requests;
using SmartCare.Application.DTOs.Responses;
using SmartCare.Application.Interfaces.Security;
using SmartCare.Application.Interfaces.Services;
using SmartCare.Domain.Constants;

namespace SmartCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IOtpService _otp;
    private readonly IJwtTokenService _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _env;
    private readonly IAuditLogger _audit;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<OtpRequest> _otpRequestValidator;
    private readonly IValidator<OtpLoginRequest> _otpLoginValidator;

    public AuthController(
        IAuthService auth,
        IOtpService otp,
        IJwtTokenService tokens,
        ICurrentUser currentUser,
        IWebHostEnvironment env,
        IAuditLogger audit,
        IValidator<LoginRequest> loginValidator,
        IValidator<OtpRequest> otpRequestValidator,
        IValidator<OtpLoginRequest> otpLoginValidator)
    {
        _auth = auth;
        _otp = otp;
        _tokens = tokens;
        _currentUser = currentUser;
        _env = env;
        _audit = audit;
        _loginValidator = loginValidator;
        _otpRequestValidator = otpRequestValidator;
        _otpLoginValidator = otpLoginValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

                var result = await _auth.LoginAsync(request.UserName, request.Password, cancellationToken);
        await _audit.LogAsync(new AuditEvent
        {
            Action = "Auth.Login",
            EntityType = "User",
            EntityId = result.User?.UserId.ToString() ?? request.UserName,
            ActorUserId = result.User?.UserId.ToString() ?? request.UserName,
            Outcome = result.Succeeded ? "Success:Password" : (result.IsLockedOut ? "Failure:Locked" : "Failure:Password")
        }, cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            var status = result.IsLockedOut ? StatusCodes.Status423Locked : StatusCodes.Status401Unauthorized;
            return StatusCode(status, new ApiMessageResponse { Message = result.Error });
        }

        return Ok(CreateAuthResponse(result.User));
    }

    [HttpPost("otp/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OtpRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequest request, CancellationToken cancellationToken)
    {
        var validation = await _otpRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

        var code = await _otp.RequestLoginOtpAsync(request.UserName, cancellationToken);

        return Ok(new OtpRequestResponse
        {
            Message = "If that account exists, a login code was written to the Outbox stub.",
            DemoCode = _env.IsDevelopment() ? code : null
        });
    }

    [HttpPost("otp/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithOtp([FromBody] OtpLoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _otpLoginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ApiMessageResponse { Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });

                var result = await _otp.LoginWithOtpAsync(request.UserName, request.Code, cancellationToken);
        await _audit.LogAsync(new AuditEvent
        {
            Action = "Auth.Login",
            EntityType = "User",
            EntityId = result.User?.UserId.ToString() ?? request.UserName,
            ActorUserId = result.User?.UserId.ToString() ?? request.UserName,
            Outcome = result.Succeeded ? "Success:OTP" : "Failure:OTP"
        }, cancellationToken);
        if (!result.Succeeded || result.User is null)
            return Unauthorized(new ApiMessageResponse { Message = result.Error });

        return Ok(CreateAuthResponse(result.User));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserName))
            return Unauthorized();

        var user = await _auth.FindByUserNameAsync(_currentUser.UserName, cancellationToken);
        if (user is null)
            return Unauthorized();

        return Ok(ToSummary(user));
    }

    [HttpGet("outbox")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.HospitalAdmin}")]
    [ProducesResponseType(typeof(IReadOnlyList<OutboxMessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Outbox(CancellationToken cancellationToken)
    {
        var messages = await _otp.ListRecentAsync(cancellationToken);
        var response = messages.Select(m => new OutboxMessageResponse
        {
            OutboxId = m.OutboxId,
            Channel = m.Channel,
            Recipient = m.Recipient,
            Subject = m.Subject,
            Body = m.Body,
            Status = m.Status,
            CreatedAtUtc = m.CreatedAtUtc
        }).ToArray();

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // JWT access tokens are discarded by the client. Server-side revocation can come later.
        return Ok(new ApiMessageResponse { Message = "Signed out. Discard the access token on the client." });
    }

    private AuthResponse CreateAuthResponse(AuthUser user)
    {
        var (token, expires) = _tokens.CreateAccessToken(user);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            User = ToSummary(user)
        };
    }

    private static UserSummaryResponse ToSummary(AuthUser user) => new()
    {
        UserId = user.UserId,
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Roles = user.Roles
    };
}
