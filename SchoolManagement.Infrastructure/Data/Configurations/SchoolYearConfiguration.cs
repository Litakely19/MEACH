using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class SchoolYearConfiguration : IEntityTypeConfiguration<SchoolYear>
{
    public void Configure(EntityTypeBuilder<SchoolYear> builder)
    {
        builder.ToTable("SchoolYears");

        builder.HasKey(y => y.Id);

        builder.Property(y => y.Name)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(y => y.StartDate).IsRequired();

        builder.Property(y => y.EndDate).IsRequired();

        builder.HasIndex(y => y.Name).IsUnique();

        builder.HasIndex(y => y.IsCurrent);
    }
}
