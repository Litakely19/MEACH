using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Data.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceiptNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(r => r.ReceiptNumber).IsUnique();

        // Exactly one receipt per payment; reprints increment PrintCount instead.
        builder.HasIndex(r => r.PaymentId).IsUnique();

        builder.HasOne(r => r.Payment)
            .WithOne(p => p.Receipt)
            .HasForeignKey<Receipt>(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.IssuedByUser)
            .WithMany()
            .HasForeignKey(r => r.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
