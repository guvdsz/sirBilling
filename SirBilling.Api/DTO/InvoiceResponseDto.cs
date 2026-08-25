public class InvoiceResponseDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public InvoiceStatus Status { get; set; }
}