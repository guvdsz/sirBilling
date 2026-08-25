public class CreateInvoiceDto
{
    public Guid SubscriptionId { get; set; }
    public DateOnly DueDate { get; set; }
}