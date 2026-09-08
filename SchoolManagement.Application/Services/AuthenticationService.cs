using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Authentication;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Authorization;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    // Deliberately identical for unknown users and wrong passwords, so the login
    // screen cannot be used to discover which usernames exist.
    private const string InvalidCredentialsMessage = "Incorrect username or password.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        IAuditService auditService,
        ILogger<AuthenticationService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var error = RequestValidation.FirstError(request, new LoginRequestValidator());
        if (error is not null)
        {
            return LoginResult.Failure(error);
        }

        var username = request.Username.Trim();
        var user = await _unitOfWork.Users.GetByUsernameAsync(username, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Username}", username);
            await _auditService.RecordAndSaveAsync(
                AuditAction.LoginFailed,
                nameof(Domain.Entities.User),
                user?.Id,
                $"Failed login attempt for '{username}'.",
                userId: user?.Id,
                cancellationToken: cancellationToken);

            return LoginResult.Failure(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            await _auditService.RecordAndSaveAsync(
                AuditAction.LoginFailed,
                nameof(Domain.Entities.User),
                user.Id,
                $"Login refused: account '{username}' is disabled.",
                userId: user.Id,
                cancellationToken: cancellationToken);

            return LoginResult.Failure("This account is disabled. Contact an administrator.");
        }

        user.LastLoginAt = DateTime.Now;

        var authenticatedUser = new AuthenticatedUser(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role.Name,
            user.Role.DisplayName,
            RolePermissionMatrix.GetPermissions(user.Role.Name).ToHashSet(),
            user.MustChangePassword);

        await _auditService.RecordAsync(
            AuditAction.Login,
            nameof(Domain.Entities.User),
            user.Id,
            $"User '{user.Username}' signed in.",
            userId: user.Id,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _currentUser.SetUser(authenticatedUser);
        _logger.LogInformation("User {Username} signed in as {Role}", user.Username, user.Role.Name);

        return LoginResult.Success(authenticatedUser);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var user = _currentUser.User;
        if (user is not null)
        {
            await _auditService.RecordAndSaveAsync(
                AuditAction.Logout,
                nameof(Domain.Entities.User),
                user.Id,
                $"User '{user.Username}' signed out.",
                userId: user.Id,
                cancellationToken: cancellationToken);

            _logger.LogInformation("User {Username} signed out", user.Username);
        }

        _currentUser.Clear();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        RequestValidation.Ensure(request, new ChangePasswordRequestValidator());

        var user = await _unitOfWork.Users.GetWithRoleAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new DomainException("The current password is incorrect.");
        }

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new DomainException("The new password must be different from the current one.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.PasswordChanged,
            nameof(Domain.Entities.User),
            user.Id,
            $"Password changed for '{user.Username}'.",
            userId: user.Id,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Refresh the session so the shell stops asking for a password change.
        if (_currentUser.User?.Id == user.Id)
        {
            _currentUser.SetUser(new AuthenticatedUser(
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Role.Name,
                user.Role.DisplayName,
                RolePermissionMatrix.GetPermissions(user.Role.Name).ToHashSet(),
                MustChangePassword: false));
        }
    }
}
