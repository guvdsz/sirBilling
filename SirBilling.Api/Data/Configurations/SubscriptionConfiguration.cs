using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.StartDate)
            .IsRequired();
        builder.Property(x => x.CanceledAt)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.CustomerId);

        builder.HasOne(x => x.Plan)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.PlanId);
    }
}