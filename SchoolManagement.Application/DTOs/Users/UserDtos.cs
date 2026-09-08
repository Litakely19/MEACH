using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Users;

public record UserListItem(
    int Id,
    string Username,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    int RoleId,
    RoleName Role,
    string RoleDisplayName,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record CreateUserRequest(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string? Email,
    int RoleId,
    bool IsActive = true,
    bool MustChangePassword = true);

public record UpdateUserRequest(
    int Id,
    string Username,
    string FirstName,
    string LastName,
    string? Email,
    int RoleId,
    bool IsActive);

public record ResetUserPasswordRequest(int UserId, string NewPassword, bool RequireChangeAtNextLogin = true);

public record RoleOption(int Id, RoleName Name, string DisplayName, string? Description);
