using SchoolManagement.Application.DTOs.Authentication;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Interfaces;

/// <summary>
/// Holds the identity of the operator for the lifetime of the desktop session and
/// is the single place where permissions are enforced.
/// </summary>
public interface ICurrentUserService
{
    AuthenticatedUser? User { get; }

    bool IsAuthenticated { get; }

    event EventHandler? UserChanged;

    void SetUser(AuthenticatedUser user);

    void Clear();

    bool HasPermission(Permission permission);

    /// <summary>Throws <see cref="Domain.Exceptions.UnauthorizedActionException"/> when the permission is missing.</summary>
    void EnsurePermission(Permission permission);

    int RequireUserId();
}
