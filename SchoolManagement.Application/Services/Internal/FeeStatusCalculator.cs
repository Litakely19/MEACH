using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Services.Internal;

internal static class FeeStatusCalculator
{
    public static FeeStatus ForFee(StudentFee fee, DateTime? today = null)
    {
        if (fee.Status == FeeStatus.Cancelled)
        {
            return FeeStatus.Cancelled;
        }

        var reference = (today ?? DateTime.Today).Date;

        if (fee.PaidAmount >= fee.ExpectedAmount)
        {
            return FeeStatus.Paid;
        }

        if (fee.DueDate.Date < reference)
        {
            return FeeStatus.Overdue;
        }

        return fee.PaidAmount > 0 ? FeeStatus.PartiallyPaid : FeeStatus.Unpaid;
    }
}
