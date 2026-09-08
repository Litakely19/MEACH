using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services.Internal;

internal static class StudentBalanceLookup
{
    public static async Task<Dictionary<int, decimal>> GetOutstandingAsync(
        IUnitOfWork unitOfWork,
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var rows = await unitOfWork.StudentFees.Query()
            .Where(fee => studentIds.Contains(fee.StudentId)
                && fee.Status != FeeStatus.Cancelled
                && fee.PaidAmount < fee.ExpectedAmount)
            .Select(fee => new
            {
                fee.StudentId,
                fee.ExpectedAmount,
                fee.PaidAmount
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.StudentId)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.ExpectedAmount - row.PaidAmount));
    }
}
