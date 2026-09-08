using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Exceptions;

public class UnauthorizedActionException : DomainException
{
    public UnauthorizedActionException(Permission permission)
        : base($"Your role is not allowed to perform this action ({permission}).")
    {
        Permission = permission;
    }

    public Permission Permission { get; }
}
