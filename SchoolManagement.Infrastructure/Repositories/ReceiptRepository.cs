using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class ReceiptRepository : Repository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<Receipt?> GetByPaymentIdAsync(
        int paymentId,
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Query(trackChanges).FirstOrDefaultAsync(receipt => receipt.PaymentId == paymentId, cancellationToken);

    public Task<Receipt?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Query()
            .Include(receipt => receipt.IssuedByUser)
            .Include(receipt => receipt.Payment)
                .ThenInclude(payment => payment.Student)
                    .ThenInclude(student => student.AcademicLevel)
            .Include(receipt => receipt.Payment)
                .ThenInclude(payment => payment.Student)
                    .ThenInclude(student => student.StudentGroup)
            .Include(receipt => receipt.Payment)
                .ThenInclude(payment => payment.ReceivedByUser)
            .Include(receipt => receipt.Payment)
                .ThenInclude(payment => payment.StudentFee)
                    .ThenInclude(fee => fee.PaymentType)
            .FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);

    public Task<string?> GetLastReceiptNumberAsync(string prefix, CancellationToken cancellationToken = default) =>
        Query()
            .Where(receipt => receipt.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(receipt => receipt.ReceiptNumber)
            .Select(receipt => receipt.ReceiptNumber)
            .FirstOrDefaultAsync(cancellationToken);
}
