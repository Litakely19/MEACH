using FluentValidation;
using SchoolManagement.Application.DTOs.Payments;

namespace SchoolManagement.Application.Validators;

public sealed class RegisterPaymentRequestValidator : AbstractValidator<RegisterPaymentRequest>
{
    public RegisterPaymentRequestValidator()
    {
        RuleFor(request => request.StudentId)
            .GreaterThan(0)
            .WithMessage("Select the student who is paying.");

        RuleFor(request => request.StudentFeeId)
            .GreaterThan(0)
            .WithMessage("Select the payment obligation to settle.");

        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("The payment amount must be greater than zero.");

        RuleFor(request => request.PaymentDate)
            .LessThanOrEqualTo(_ => DateTime.Today)
            .WithMessage("The payment date cannot be in the future.");
    }
}

public sealed class CancelPaymentRequestValidator : AbstractValidator<CancelPaymentRequest>
{
    public CancelPaymentRequestValidator()
    {
        RuleFor(request => request.PaymentId).GreaterThan(0);

        RuleFor(request => request.Reason)
            .NotEmpty()
            .WithMessage("A reason is required to cancel a payment.");
    }
}
