using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(student => student.Id);

        builder.Property(student => student.StudentNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(student => student.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(student => student.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(student => student.Gender)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(student => student.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(student => student.Address)
            .HasMaxLength(512);

        builder.Property(student => student.PhoneNumber)
            .HasMaxLength(32);

        builder.Property(student => student.Email)
            .HasMaxLength(256);

        builder.Ignore(student => student.FullName);

        builder.HasIndex(student => student.StudentNumber).IsUnique();

        builder.HasIndex(student => student.LastName);

        builder.HasIndex(student => new { student.AcademicLevelId, student.StudentGroupId, student.Status });

        builder.HasIndex(student => student.IsDeleted);

        builder.HasOne(student => student.AcademicLevel)
            .WithMany(level => level.Students)
            .HasForeignKey(student => student.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(student => student.StudentGroup)
            .WithMany(group => group.Students)
            .HasForeignKey(student => student.StudentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(student => student.SchoolYear)
            .WithMany(year => year.Students)
            .HasForeignKey(student => student.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
