using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Users;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<UserListItem>> ListAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewUsers);

        var query = _unitOfWork.Users.Query().Include(user => user.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var names = PersonNameSearch.Parse(searchTerm);
            var lower = names.Lower;
            var firstPart = names.FirstPart;
            var lastPart = names.LastPart;
            var hasTwoParts = names.HasTwoParts;

            query = query.Where(user =>
                user.Username.ToLower().Contains(lower)
                || user.FirstName.ToLower().Contains(lower)
                || user.LastName.ToLower().Contains(lower)
                || (user.FirstName + " " + user.LastName).ToLower().Contains(lower)
                || (user.LastName + " " + user.FirstName).ToLower().Contains(lower)
                || (hasTwoParts
                    && user.FirstName.ToLower().Contains(firstPart)
                    && user.LastName.ToLower().Contains(lastPart))
                || (user.Email != null && user.Email.ToLower().Contains(lower)));
        }

        var users = await query
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);

        return users.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<RoleOption>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _unitOfWork.Roles.Query()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleOption(role.Id, role.Name, role.DisplayName, role.Description))
            .ToList();
    }

    public async Task<int> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageUsers);
        RequestValidation.Ensure(request, new CreateUserRequestValidator());

        var username = request.Username.Trim();
        if (await _unitOfWork.Users.UsernameExistsAsync(username, cancellationToken: cancellationToken))
        {
            throw new DomainException($"The username '{username}' is already taken.");
        }

        _ = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = NormalizeOptional(request.Email),
            RoleId = request.RoleId,
            IsActive = request.IsActive,
            MustChangePassword = request.MustChangePassword,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(User),
            null,
            $"User '{username}' created.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageUsers);

        var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        var username = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("The username is required.");
        }

        if (await _unitOfWork.Users.UsernameExistsAsync(username, request.Id, cancellationToken))
        {
            throw new DomainException($"The username '{username}' is already taken.");
        }

        // Keeping at least one enabled administrator is what prevents locking
        // everybody out of the application.
        if (user.Id == _currentUser.User?.Id && !request.IsActive)
        {
            throw new DomainException("You cannot disable your own account.");
        }

        await EnsureNotLastActiveAdministratorAsync(user, request.RoleId, request.IsActive, cancellationToken);

        user.Username = username;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = NormalizeOptional(request.Email);
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(User),
            user.Id,
            $"User '{user.Username}' updated.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageUsers);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (user.Id == _currentUser.User?.Id && !isActive)
        {
            throw new DomainException("You cannot disable your own account.");
        }

        await EnsureNotLastActiveAdministratorAsync(user, user.RoleId, isActive, cancellationToken);

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(User),
            user.Id,
            $"User '{user.Username}' {(isActive ? "enabled" : "disabled")}.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageUsers);

        RequestValidation.Ensure(request, new ResetUserPasswordRequestValidator());

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = request.RequireChangeAtNextLogin;
        user.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.PasswordChanged,
            nameof(User),
            user.Id,
            $"Password reset for '{user.Username}'.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNotLastActiveAdministratorAsync(
        User user,
        int newRoleId,
        bool newIsActive,
        CancellationToken cancellationToken)
    {
        var administratorRole = await _unitOfWork.Roles.GetByNameAsync(RoleName.Administrator, cancellationToken);
        if (administratorRole is null || user.RoleId != administratorRole.Id)
        {
            return;
        }

        var staysActiveAdministrator = newIsActive && newRoleId == administratorRole.Id;
        if (staysActiveAdministrator)
        {
            return;
        }

        var otherActiveAdministrators = await _unitOfWork.Users.Query()
            .CountAsync(
                candidate => candidate.RoleId == administratorRole.Id
                    && candidate.IsActive
                    && candidate.Id != user.Id,
                cancellationToken);

        if (otherActiveAdministrators == 0)
        {
            throw new DomainException(
                "This is the last active administrator account; it must keep its role and stay enabled.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserListItem Map(User user) =>
        new(
            user.Id,
            user.Username,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email,
            user.RoleId,
            user.Role.Name,
            user.Role.DisplayName,
            user.IsActive,
            user.MustChangePassword,
            user.LastLoginAt,
            user.CreatedAt);
}
