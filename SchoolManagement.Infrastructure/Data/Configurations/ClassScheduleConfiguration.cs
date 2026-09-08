using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class ClassScheduleConfiguration : IEntityTypeConfiguration<ClassSchedule>
{
    public void Configure(EntityTypeBuilder<ClassSchedule> builder)
    {
        builder.ToTable("ClassSchedules");

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(schedule => schedule.SessionLabel);

        builder.HasIndex(schedule => new { schedule.StudentGroupId, schedule.DayOfWeek, schedule.StartTime });

        builder.HasOne(schedule => schedule.StudentGroup)
            .WithMany(group => group.Schedules)
            .HasForeignKey(schedule => schedule.StudentGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
