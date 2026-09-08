using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class SchoolSettingConfiguration : IEntityTypeConfiguration<SchoolSetting>
{
    public void Configure(EntityTypeBuilder<SchoolSetting> builder)
    {
        builder.ToTable("SchoolSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SchoolName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Address)
            .HasMaxLength(512);

        builder.Property(s => s.PhoneNumber)
            .HasMaxLength(64);

        builder.Property(s => s.Email)
            .HasMaxLength(256);

        builder.Property(s => s.Website)
            .HasMaxLength(256);

        builder.Property(s => s.CurrencyCode)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(s => s.CurrencySymbol)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(s => s.ReceiptFooter)
            .HasMaxLength(512);

        builder.Property(s => s.LogoPath)
            .HasMaxLength(512);
    }
}
