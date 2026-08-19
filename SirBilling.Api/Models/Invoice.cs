public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; } = 0;
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public Subscription Subscription { get; set; } = null!;
}