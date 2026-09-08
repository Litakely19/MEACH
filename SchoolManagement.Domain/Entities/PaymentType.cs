using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class PaymentType : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal DefaultAmount { get; set; }

    public PaymentFrequency Frequency { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<StudentFee> StudentFees { get; set; } = new List<StudentFee>();
}
