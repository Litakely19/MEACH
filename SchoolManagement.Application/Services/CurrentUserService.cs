using SchoolManagement.Application.DTOs.Authentication;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;

namespace SchoolManagement.Application.Services;

/// <summary>
/// Session state for the desktop process. Registered as a singleton: one signed in
/// operator per running application.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private AuthenticatedUser? _user;

    public AuthenticatedUser? User => _user;

    public bool IsAuthenticated => _user is not null;

    public event EventHandler? UserChanged;

    public void SetUser(AuthenticatedUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _user = user;
        UserChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _user = null;
        UserChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasPermission(Permission permission) => _user?.HasPermission(permission) ?? false;

    public void EnsurePermission(Permission permission)
    {
        if (!HasPermission(permission))
        {
            throw new UnauthorizedActionException(permission);
        }
    }

    public int RequireUserId() =>
        _user?.Id ?? throw new DomainException("No user is signed in.");
}
