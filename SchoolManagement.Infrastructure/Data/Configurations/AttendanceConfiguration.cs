using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");

        builder.HasKey(attendance => attendance.Id);

        builder.Property(attendance => attendance.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(attendance => attendance.Remarks)
            .HasMaxLength(512);

        builder.HasIndex(attendance => new
            {
                attendance.StudentId,
                attendance.ClassScheduleId,
                attendance.AttendanceDate
            })
            .IsUnique();

        builder.HasIndex(attendance => attendance.AttendanceDate);

        builder.HasOne(attendance => attendance.Student)
            .WithMany(student => student.Attendances)
            .HasForeignKey(attendance => attendance.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attendance => attendance.ClassSchedule)
            .WithMany(schedule => schedule.Attendances)
            .HasForeignKey(attendance => attendance.ClassScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attendance => attendance.RecordedByUser)
            .WithMany(user => user.RecordedAttendances)
            .HasForeignKey(attendance => attendance.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
