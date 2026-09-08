using SchoolManagement.Application.DTOs.Authentication;

namespace SchoolManagement.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
