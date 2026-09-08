using FluentValidation;
using SchoolManagement.Domain.Exceptions;

namespace SchoolManagement.Application.Validators;

/// <summary>
/// Turns a FluentValidation failure into the same <see cref="DomainException"/>
/// the rest of the application already surfaces to the UI.
/// </summary>
public static class RequestValidation
{
    public static void Ensure<T>(T instance, IValidator<T> validator)
    {
        var result = validator.Validate(instance);
        if (!result.IsValid)
        {
            throw new DomainException(result.Errors[0].ErrorMessage);
        }
    }

    public static string? FirstError<T>(T instance, IValidator<T> validator)
    {
        var result = validator.Validate(instance);
        return result.IsValid ? null : result.Errors[0].ErrorMessage;
    }
}
