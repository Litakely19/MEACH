using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class PaymentTypeRepository : Repository<PaymentType>, IPaymentTypeRepository
{
    public PaymentTypeRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<PaymentType?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Query(trackChanges: true).FirstOrDefaultAsync(type => type.Name == name, cancellationToken);

    public async Task<IReadOnlyList<PaymentType>> ListActiveAsync(
        PaymentFrequency? frequency = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(type => type.IsActive);

        if (frequency.HasValue)
        {
            query = query.Where(type => type.Frequency == frequency.Value);
        }

        return await query.OrderBy(type => type.Name).ToListAsync(cancellationToken);
    }
}
