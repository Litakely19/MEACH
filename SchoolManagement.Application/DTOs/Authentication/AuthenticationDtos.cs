using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Authentication;

public record LoginRequest(string Username, string Password);

/// <summary>
/// Identity of the signed in operator, carrying the resolved permission set so the
/// UI can enable or hide features without another round trip.
/// </summary>
public record AuthenticatedUser(
    int Id,
    string Username,
    string FullName,
    string? Email,
    RoleName Role,
    string RoleDisplayName,
    IReadOnlySet<Permission> Permissions,
    bool MustChangePassword)
{
    public bool HasPermission(Permission permission) => Permissions.Contains(permission);
}

public record LoginResult(bool Succeeded, AuthenticatedUser? User, string? ErrorMessage)
{
    public static LoginResult Success(AuthenticatedUser user) => new(true, user, null);

    public static LoginResult Failure(string message) => new(false, null, message);
}

public record ChangePasswordRequest(int UserId, string CurrentPassword, string NewPassword, string ConfirmPassword);
