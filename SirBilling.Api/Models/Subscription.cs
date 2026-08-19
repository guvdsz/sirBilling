public class Subscription
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? CanceledAt { get; set; }
    public Customer Customer { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}