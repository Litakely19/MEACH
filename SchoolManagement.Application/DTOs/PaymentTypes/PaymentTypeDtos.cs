using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.PaymentTypes;

public record PaymentTypeListItem(
    int Id,
    string Name,
    string? Description,
    decimal DefaultAmount,
    PaymentFrequency Frequency,
    bool IsActive);

public record CreatePaymentTypeRequest(
    string Name,
    string? Description,
    decimal DefaultAmount,
    PaymentFrequency Frequency);

public record UpdatePaymentTypeRequest(
    int Id,
    string Name,
    string? Description,
    decimal DefaultAmount,
    PaymentFrequency Frequency,
    bool IsActive);

public record PaymentTypeOption(int Id, string Name, decimal DefaultAmount, PaymentFrequency Frequency);
