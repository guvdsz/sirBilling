public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; } = 0;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public Invoice Invoice { get; set; } = null!;
}