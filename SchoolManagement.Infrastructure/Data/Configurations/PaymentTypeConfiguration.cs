using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class PaymentTypeConfiguration : IEntityTypeConfiguration<PaymentType>
{
    public void Configure(EntityTypeBuilder<PaymentType> builder)
    {
        builder.ToTable("PaymentTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(512);

        builder.Property(t => t.DefaultAmount).IsRequired();

        builder.Property(t => t.Frequency)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
