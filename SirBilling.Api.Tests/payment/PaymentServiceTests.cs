using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Tests.PaymentTests;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenInvoiceIsPending_CreatesPaymentAndMarksInvoiceAsPaid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;

        var service = new PaymentService(db);

        var invoice = new Invoice
        {
            Amount = 99.90m,
            DueDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            ),
            Status = InvoiceStatus.Pending,
            Subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                Customer = new Customer
                {
                    Name = "Ana",
                    Email = "ana@example.com"
                },
                Plan = new Plan
                {
                    Name = "Pro",
                    Price = 99.90m
                }
            }
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var result = await service.CreateAsync(invoice.Id);

        db.ChangeTracker.Clear();

        var savedInvoice = await db.Invoices.SingleAsync(x => x.Id == invoice.Id);

        var savedPayment = await db.Payments.SingleAsync(x => x.InvoiceId == invoice.Id);

        Assert.Equal(InvoiceStatus.Paid, savedInvoice.Status);
        Assert.Equal(invoice.Amount, savedPayment.Amount);
        Assert.Equal(CreatePaymentStatus.Success, result.Status);

        var returnedPayment = Assert.IsType<Payment>(result.Payment);
        Assert.Equal(invoice.Id, returnedPayment.InvoiceId);
    }

    [Fact]
    public async Task CreateAsync_WhenInvoiceIsAlreadyPaid_ReturnsInvoiceAlreadyPaid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;

        var service = new PaymentService(db);

        var invoice = new Invoice
        {
            Amount = 99.90m,
            DueDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            ),
            Status = InvoiceStatus.Pending,
            Subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                Customer = new Customer
                {
                    Name = "Ana",
                    Email = "ana@example.com"
                },
                Plan = new Plan
                {
                    Name = "Pro",
                    Price = 99.90m
                }
            }
        };

        var existingPayment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = invoice.Amount,
            PaidAt = DateTime.UtcNow
        };

        invoice.MarkAsPaid();

        db.Invoices.Add(invoice);
        db.Payments.Add(existingPayment);


        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var result = await service.CreateAsync(invoice.Id);

        var savedInvoice = await db.Invoices.SingleAsync(x => x.Id == invoice.Id);
        var paymentCount = await db.Payments.CountAsync(x => x.InvoiceId == invoice.Id);

        Assert.Equal(1, paymentCount);
        Assert.Equal(InvoiceStatus.Paid, savedInvoice.Status);
        Assert.Null(result.Payment);
        Assert.Equal(CreatePaymentStatus.InvoiceAlreadyPaid, result.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenInvoiceIsCanceled_ReturnsInvoiceCanceled()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;

        var service = new PaymentService(db);

        var invoice = new Invoice
        {
            Amount = 99.90m,
            DueDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            ),
            Status = InvoiceStatus.Pending,
            Subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                Customer = new Customer
                {
                    Name = "Ana",
                    Email = "ana@example.com"
                },
                Plan = new Plan
                {
                    Name = "Pro",
                    Price = 99.90m
                }
            }
        };

        db.Invoices.Add(invoice);

        invoice.Cancel();

        await db.SaveChangesAsync();

        var result = await service.CreateAsync(invoice.Id);

        db.ChangeTracker.Clear();

        var paymentExists = await db.Payments.AnyAsync();

        Assert.False(paymentExists);
        Assert.Null(result.Payment);
        Assert.Equal(CreatePaymentStatus.InvoiceCanceled, result.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenInvoiceDoesNotExist_ReturnsInvoiceNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;

        var service = new PaymentService(db);

        var result = await service.CreateAsync(Guid.NewGuid());

        var paymentExists = await db.Payments.AnyAsync();

        Assert.False(paymentExists);
        Assert.Null(result.Payment);
        Assert.Equal(CreatePaymentStatus.InvoiceNotFound, result.Status);

    }
}