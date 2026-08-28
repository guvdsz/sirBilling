using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .IsRequired();

        builder.HasOne(x => x.Invoice)
            .WithOne(x => x.Payment)
            .HasForeignKey<Payment>(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}