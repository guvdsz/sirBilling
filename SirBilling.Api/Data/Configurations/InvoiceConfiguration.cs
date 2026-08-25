using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubscriptionId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.SubscriptionId);
    }
}