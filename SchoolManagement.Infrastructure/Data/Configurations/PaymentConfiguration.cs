using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.PaymentNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(payment => payment.Amount).IsRequired();

        builder.Property(payment => payment.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(payment => payment.Reference)
            .HasMaxLength(128);

        builder.Property(payment => payment.Notes)
            .HasMaxLength(1024);

        builder.Property(payment => payment.CancellationReason)
            .HasMaxLength(512);

        builder.HasIndex(payment => payment.PaymentNumber).IsUnique();

        builder.HasIndex(payment => payment.PaymentDate);

        builder.HasIndex(payment => new { payment.StudentId, payment.Status });

        builder.HasIndex(payment => payment.StudentFeeId);

        builder.HasIndex(payment => payment.ReceivedByUserId);

        builder.HasOne(payment => payment.Student)
            .WithMany(student => student.Payments)
            .HasForeignKey(payment => payment.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(payment => payment.StudentFee)
            .WithMany(fee => fee.Payments)
            .HasForeignKey(payment => payment.StudentFeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(payment => payment.ReceivedByUser)
            .WithMany(user => user.ReceivedPayments)
            .HasForeignKey(payment => payment.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(payment => payment.CancelledByUser)
            .WithMany()
            .HasForeignKey(payment => payment.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
