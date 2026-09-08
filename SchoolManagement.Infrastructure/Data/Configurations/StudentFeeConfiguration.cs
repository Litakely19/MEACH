using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class StudentFeeConfiguration : IEntityTypeConfiguration<StudentFee>
{
    public void Configure(EntityTypeBuilder<StudentFee> builder)
    {
        builder.ToTable("StudentFees");

        builder.HasKey(fee => fee.Id);

        builder.Property(fee => fee.ExpectedAmount).IsRequired();

        builder.Property(fee => fee.PaidAmount).IsRequired();

        builder.Property(fee => fee.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(fee => fee.Notes)
            .HasMaxLength(1024);

        builder.Ignore(fee => fee.RemainingAmount);

        builder.HasIndex(fee => new { fee.StudentId, fee.PaymentTypeId, fee.Month, fee.Year });

        builder.HasIndex(fee => new { fee.StudentId, fee.PaymentTypeId, fee.SchoolYearId });

        builder.HasIndex(fee => fee.Status);

        builder.HasIndex(fee => fee.DueDate);

        builder.HasOne(fee => fee.Student)
            .WithMany(student => student.Fees)
            .HasForeignKey(fee => fee.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fee => fee.PaymentType)
            .WithMany(type => type.StudentFees)
            .HasForeignKey(fee => fee.PaymentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fee => fee.SchoolYear)
            .WithMany(year => year.StudentFees)
            .HasForeignKey(fee => fee.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(fee => fee.CreatedByUser)
            .WithMany(user => user.CreatedFees)
            .HasForeignKey(fee => fee.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
