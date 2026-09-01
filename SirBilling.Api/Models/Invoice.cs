public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CanceledAt { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public Payment? Payment { get; set; } = null!;

    public void MarkAsPaid()
    {
        if (Status != InvoiceStatus.Pending && Status != InvoiceStatus.Overdue)
        {
            throw new InvalidOperationException(
                "Only pending or overdue invoices can be paid."
            );
        }

        Status = InvoiceStatus.Paid;
    }

    public void Cancel()
    {
        if (Status != InvoiceStatus.Pending && Status != InvoiceStatus.Overdue)
        {
            throw new InvalidOperationException(
                "Only pending or overdue invoices can be canceled."
            );
        }

        Status = InvoiceStatus.Canceled;
        CanceledAt = DateTime.UtcNow;
    }
}