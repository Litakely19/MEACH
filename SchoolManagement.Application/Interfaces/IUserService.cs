using SchoolManagement.Application.DTOs.Users;

namespace SchoolManagement.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserListItem>> ListAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleOption>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
}
