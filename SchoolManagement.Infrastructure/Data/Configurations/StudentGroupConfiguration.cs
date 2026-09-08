using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.ToTable("StudentGroups");

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(group => group.Description)
            .HasMaxLength(512);

        builder.HasIndex(group => new { group.AcademicLevelId, group.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

        builder.HasIndex(group => group.IsDeleted);

        builder.HasOne(group => group.AcademicLevel)
            .WithMany(level => level.StudentGroups)
            .HasForeignKey(group => group.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
