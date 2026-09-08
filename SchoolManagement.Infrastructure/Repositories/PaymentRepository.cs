using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<Payment?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Query()
            .Include(payment => payment.Student)
                .ThenInclude(student => student.AcademicLevel)
            .Include(payment => payment.Student)
                .ThenInclude(student => student.StudentGroup)
            .Include(payment => payment.StudentFee)
                .ThenInclude(fee => fee.PaymentType)
            .Include(payment => payment.ReceivedByUser)
            .Include(payment => payment.Receipt)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public Task<string?> GetLastPaymentNumberAsync(string prefix, CancellationToken cancellationToken = default) =>
        Query()
            .Where(payment => payment.PaymentNumber.StartsWith(prefix))
            .OrderByDescending(payment => payment.PaymentNumber)
            .Select(payment => payment.PaymentNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Payment?> FindPossibleDuplicateAsync(
        int studentId,
        decimal amount,
        DateTime paymentDate,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var from = paymentDate - window;
        var to = paymentDate + window;

        return Query()
            .Where(payment => payment.StudentId == studentId
                && payment.Amount == amount
                && payment.Status == PaymentStatus.Active
                && payment.PaymentDate >= from
                && payment.PaymentDate <= to)
            .OrderByDescending(payment => payment.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
