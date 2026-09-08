using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.DisplayName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(256);

        builder.HasIndex(r => r.Name).IsUnique();
    }
}
