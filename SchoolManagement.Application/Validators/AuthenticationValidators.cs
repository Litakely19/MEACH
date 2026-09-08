using FluentValidation;
using SchoolManagement.Application.DTOs.Authentication;

namespace SchoolManagement.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Username)
            .NotEmpty()
            .WithMessage("Enter your username and your password.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Enter your username and your password.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.UserId).GreaterThan(0);

        RuleFor(request => request.CurrentPassword)
            .NotEmpty()
            .WithMessage("The current password is required.");

        RuleFor(request => request.NewPassword)
            .Must(password => PasswordPolicy.IsValid(password, out _))
            .WithMessage((_, password) =>
            {
                PasswordPolicy.IsValid(password, out var error);
                return error ?? PasswordPolicy.Describe();
            });

        RuleFor(request => request.ConfirmPassword)
            .Equal(request => request.NewPassword)
            .WithMessage("The new password and its confirmation do not match.");
    }
}
