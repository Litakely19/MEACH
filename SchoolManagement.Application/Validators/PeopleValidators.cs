using FluentValidation;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.DTOs.SchoolYears;

namespace SchoolManagement.Application.Validators;

public sealed class CreateSchoolYearRequestValidator : AbstractValidator<CreateSchoolYearRequest>
{
    public CreateSchoolYearRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("The school year name is required.");

        RuleFor(request => request)
            .Must(request => request.EndDate.Date > request.StartDate.Date)
            .WithMessage("The end date must be later than the start date.");
    }
}

public sealed class UpdateSchoolYearRequestValidator : AbstractValidator<UpdateSchoolYearRequest>
{
    public UpdateSchoolYearRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);

        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("The school year name is required.");

        RuleFor(request => request)
            .Must(request => request.EndDate.Date > request.StartDate.Date)
            .WithMessage("The end date must be later than the start date.");
    }
}

public sealed class CreatePaymentTypeRequestValidator : AbstractValidator<CreatePaymentTypeRequest>
{
    public CreatePaymentTypeRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("The payment type name is required.");

        RuleFor(request => request.DefaultAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("The default amount cannot be negative.");
    }
}
