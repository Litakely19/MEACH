using FluentValidation;
using SchoolManagement.Application.DTOs.Students;

namespace SchoolManagement.Application.Validators;

public sealed class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithMessage("The first name and the last name are required.");

        RuleFor(request => request.LastName)
            .NotEmpty()
            .WithMessage("The first name and the last name are required.");

        RuleFor(request => request.DateOfBirth)
            .LessThan(DateTime.Today)
            .WithMessage("The date of birth must be in the past.");

        RuleFor(request => request)
            .Must(request => request.EnrollmentDate.Date >= request.DateOfBirth.Date)
            .WithMessage("The enrollment date cannot precede the date of birth.");

        RuleFor(request => request.AcademicLevelId)
            .GreaterThan(0)
            .WithMessage("Select an academic level.");

        RuleFor(request => request.StudentGroupId)
            .GreaterThan(0)
            .WithMessage("Select a student group.");
    }
}

public sealed class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithMessage("The first name and the last name are required.");

        RuleFor(request => request.LastName)
            .NotEmpty()
            .WithMessage("The first name and the last name are required.");

        RuleFor(request => request.DateOfBirth)
            .LessThan(DateTime.Today)
            .WithMessage("The date of birth must be in the past.");

        RuleFor(request => request)
            .Must(request => request.EnrollmentDate.Date >= request.DateOfBirth.Date)
            .WithMessage("The enrollment date cannot precede the date of birth.");

        RuleFor(request => request.AcademicLevelId)
            .GreaterThan(0)
            .WithMessage("Select an academic level.");

        RuleFor(request => request.StudentGroupId)
            .GreaterThan(0)
            .WithMessage("Select a student group.");
    }
}
