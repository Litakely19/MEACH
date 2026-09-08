using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class AcademicLevelConfiguration : IEntityTypeConfiguration<AcademicLevel>
{
    public void Configure(EntityTypeBuilder<AcademicLevel> builder)
    {
        builder.ToTable("AcademicLevels");

        builder.HasKey(level => level.Id);

        builder.Property(level => level.Name)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(level => level.Description)
            .HasMaxLength(512);

        builder.HasIndex(level => level.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

        builder.HasIndex(level => level.IsDeleted);
    }
}
