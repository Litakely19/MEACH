using FluentValidation;
using SchoolManagement.Application.DTOs.Users;

namespace SchoolManagement.Application.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Username)
            .NotEmpty()
            .WithMessage("The username is required.");

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithMessage("The first name is required.");

        RuleFor(request => request.LastName)
            .NotEmpty()
            .WithMessage("The last name is required.");

        RuleFor(request => request.Password)
            .Must(password => PasswordPolicy.IsValid(password, out _))
            .WithMessage((_, password) =>
            {
                PasswordPolicy.IsValid(password, out var error);
                return error ?? PasswordPolicy.Describe();
            });

        RuleFor(request => request.RoleId)
            .GreaterThan(0)
            .WithMessage("Select a role.");
    }
}

public sealed class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(request => request.UserId).GreaterThan(0);

        RuleFor(request => request.NewPassword)
            .Must(password => PasswordPolicy.IsValid(password, out _))
            .WithMessage((_, password) =>
            {
                PasswordPolicy.IsValid(password, out var error);
                return error ?? PasswordPolicy.Describe();
            });
    }
}
