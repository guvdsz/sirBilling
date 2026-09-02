using Microsoft.EntityFrameworkCore;

public enum CreatePaymentStatus
{
    Success,
    InvoiceNotFound,
    InvoiceAlreadyPaid,
    InvoiceCanceled
}

public sealed record CreatePaymentResult(
    CreatePaymentStatus Status,
    Payment? Payment = null
);

public sealed class PaymentService
{
    private readonly ApplicationDbContext _db;

    public PaymentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CreatePaymentResult> CreateAsync(Guid invoiceId)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);

        if (invoice == null)
        {
            return new CreatePaymentResult(CreatePaymentStatus.InvoiceNotFound);
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return new CreatePaymentResult(CreatePaymentStatus.InvoiceAlreadyPaid);
        }

        if (invoice.Status == InvoiceStatus.Canceled)
        {
            return new CreatePaymentResult(CreatePaymentStatus.InvoiceCanceled);
        }

        var payment = new Payment
        {
            InvoiceId = invoiceId,
            Amount = invoice.Amount
        };

        invoice.MarkAsPaid();

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return new CreatePaymentResult(CreatePaymentStatus.Success, payment);
    }
}