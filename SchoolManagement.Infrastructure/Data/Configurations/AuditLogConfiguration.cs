using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(64);

        builder.Property(a => a.Description)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(a => a.CreatedAt);

        builder.HasIndex(a => new { a.EntityName, a.EntityId });

        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
