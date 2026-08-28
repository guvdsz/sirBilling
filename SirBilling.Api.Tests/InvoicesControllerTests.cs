using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirBilling.Api.Controllers;

namespace SirBilling.Api.Tests;

public class InvoicesControllerTests
{
    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await new InvoiceController(database.Context).GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSubscriptionIsMissing_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await new InvoiceController(database.Context).Create(new CreateInvoiceDto
        {
            SubscriptionId = Guid.NewGuid(),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenDueDateIsNotFuture_ReturnsBadRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        var subscription = await AddActiveSubscriptionAsync(database.Context);

        var result = await new InvoiceController(database.Context).Create(new CreateInvoiceDto
        {
            SubscriptionId = subscription.Id,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenValid_UsesPlanPriceAndPendingStatus()
    {
        await using var database = await TestDatabase.CreateAsync();
        var subscription = await AddActiveSubscriptionAsync(database.Context, 149m);

        var result = await new InvoiceController(database.Context).Create(new CreateInvoiceDto
        {
            SubscriptionId = subscription.Id,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<InvoiceResponseDto>(created.Value);
        Assert.Equal(149m, response.Amount);
        Assert.Equal(InvoiceStatus.Pending, response.Status);
        Assert.True(await database.Context.Invoices.AnyAsync(x => x.Id == response.Id));
    }

    [Fact]
    public async Task Create_WhenSubscriptionIsInactive_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var subscription = await AddActiveSubscriptionAsync(database.Context);
        subscription.Status = SubscriptionStatus.Canceled;
        await database.Context.SaveChangesAsync();

        var result = await new InvoiceController(database.Context).Create(new CreateInvoiceDto
        {
            SubscriptionId = subscription.Id,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(await database.Context.Invoices.ToListAsync());
    }

    [Fact]
    public async Task Cancel_WhenPending_SetsCanceledStatusAndDate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var subscription = await AddActiveSubscriptionAsync(database.Context);
        var invoice = new Invoice
        {
            SubscriptionId = subscription.Id,
            Amount = 99m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Status = InvoiceStatus.Pending
        };
        database.Context.Invoices.Add(invoice);
        await database.Context.SaveChangesAsync();

        var result = await new InvoiceController(database.Context).Cancel(invoice.Id);

        Assert.IsType<NoContentResult>(result);
        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.Invoices
            .SingleAsync(x => x.Id == invoice.Id);
        Assert.Equal(InvoiceStatus.Canceled, saved.Status);
        Assert.NotNull(saved.CanceledAt);
    }

    [Fact]
    public async Task Cancel_WhenPaid_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var subscription = await AddActiveSubscriptionAsync(database.Context);
        var invoice = new Invoice
        {
            SubscriptionId = subscription.Id,
            Amount = 99m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Status = InvoiceStatus.Paid
        };
        database.Context.Invoices.Add(invoice);
        await database.Context.SaveChangesAsync();

        var result = await new InvoiceController(database.Context).Cancel(invoice.Id);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenInvoiceIsAlreadyCanceled_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var subscription = await AddActiveSubscriptionAsync(database.Context);
        var invoice = new Invoice
        {
            SubscriptionId = subscription.Id,
            Amount = 99m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Status = InvoiceStatus.Canceled,
            CanceledAt = DateTime.UtcNow.AddMinutes(-5)
        };
        database.Context.Invoices.Add(invoice);
        await database.Context.SaveChangesAsync();

        var result = await new InvoiceController(database.Context).Cancel(invoice.Id);

        Assert.IsType<ConflictObjectResult>(result);
        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.Invoices.SingleAsync(x => x.Id == invoice.Id);
        Assert.Equal(InvoiceStatus.Canceled, saved.Status);
        Assert.NotNull(saved.CanceledAt);
    }

    [Fact]
    public async Task Cancel_WhenInvoiceDoesNotExist_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await new InvoiceController(database.Context).Cancel(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static async Task<Subscription> AddActiveSubscriptionAsync(ApplicationDbContext context, decimal price = 99m)
    {
        var subscription = new Subscription
        {
            Customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" },
            Plan = new Plan { Name = "Pro", Price = price }
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        return subscription;
    }
}
