public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? CanceledAt { get; set; }
}