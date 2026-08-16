public class Subscription
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? CanceledAt { get; set; }
    public Customer Customer { get; set; }
    public Plan Plan { get; set; }
}