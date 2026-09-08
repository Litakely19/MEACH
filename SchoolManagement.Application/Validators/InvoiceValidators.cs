using FluentValidation;
using SchoolManagement.Application.DTOs.Fees;

namespace SchoolManagement.Application.Validators;

public sealed class CreateStudentFeeRequestValidator : AbstractValidator<CreateStudentFeeRequest>
{
    public CreateStudentFeeRequestValidator()
    {
        RuleFor(request => request.StudentId)
            .GreaterThan(0)
            .WithMessage("Select the student.");

        RuleFor(request => request.PaymentTypeId)
            .GreaterThan(0)
            .WithMessage("Select the payment type.");

        RuleFor(request => request.ExpectedAmount)
            .GreaterThan(0)
            .WithMessage("The expected amount must be greater than zero.");
    }
}

public sealed class AdjustStudentFeeRequestValidator : AbstractValidator<AdjustStudentFeeRequest>
{
    public AdjustStudentFeeRequestValidator()
    {
        RuleFor(request => request.StudentFeeId).GreaterThan(0);

        RuleFor(request => request.ExpectedAmount)
            .GreaterThan(0)
            .WithMessage("The expected amount must be greater than zero.");
    }
}

public sealed class GenerateMonthlyFeesRequestValidator : AbstractValidator<GenerateMonthlyFeesRequest>
{
    public GenerateMonthlyFeesRequestValidator()
    {
        RuleFor(request => request.PaymentTypeId).GreaterThan(0);

        RuleFor(request => request.Months)
            .NotEmpty()
            .WithMessage("Select at least one month to bill.");

        RuleFor(request => request.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Select a valid year.");

        RuleFor(request => request.AmountPerMonth)
            .GreaterThan(0)
            .WithMessage("The monthly amount must be greater than zero.");

        RuleFor(request => request.DueDay)
            .InclusiveBetween(1, 28)
            .WithMessage("The due day must be between 1 and 28.");
    }
}

public sealed class GenerateOneTimeFeeRequestValidator : AbstractValidator<GenerateOneTimeFeeRequest>
{
    public GenerateOneTimeFeeRequestValidator()
    {
        RuleFor(request => request.PaymentTypeId).GreaterThan(0);

        RuleFor(request => request.SchoolYearId)
            .GreaterThan(0)
            .WithMessage("Select the school year.");

        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("The amount must be greater than zero.");
    }
}
