using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SirBilling.Api.Controllers;

namespace SirBilling.Api.Tests;

public class PaymentsControllerTests
{
    [Fact]
    public async Task Create_WhenInvoiceIsPending_CreatesPaymentAndMarksInvoiceAsPaid()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);

        await db.Database.EnsureCreatedAsync();

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

        var controller = new PaymentsController(db);

        var request = new CreatePaymentDto
        {
            InvoiceId = invoice.Id
        };

        // Act
        var result = await controller.Create(request);

        // Assert: resposta HTTP
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        var response =
            Assert.IsType<PaymentResponseDto>(createdResult.Value);

        Assert.Equal(invoice.Id, response.InvoiceId);
        Assert.Equal(99.90m, response.Amount);

        // Remove as entidades acompanhadas para reler o banco
        db.ChangeTracker.Clear();

        var savedInvoice = await db.Invoices
            .SingleAsync(x => x.Id == invoice.Id);

        var savedPayment = await db.Payments
            .SingleAsync(x => x.InvoiceId == invoice.Id);

        Assert.Equal(InvoiceStatus.Paid, savedInvoice.Status);
        Assert.Equal(invoice.Id, savedPayment.InvoiceId);
        Assert.Equal(invoice.Amount, savedPayment.Amount);
    }

    [Fact]
    public async Task Create_WhenInvoiceDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var controller = new PaymentsController(db);

        var request = new CreatePaymentDto
        {
            InvoiceId = Guid.NewGuid() // ID de fatura inexistente
        };

        // Act
        var result = await controller.Create(request);

        db.ChangeTracker.Clear();

        // Assert
        var paymentWasCreated = await db.Payments.AnyAsync();

        Assert.False(paymentWasCreated);

        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenInvoiceIsCanceled_ReturnsConflict()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var controller = new PaymentsController(db);

        var invoice = new Invoice
        {
            Amount = 99.90m,
            DueDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            ),
            Status = InvoiceStatus.Canceled,
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

        var request = new CreatePaymentDto
        {
            InvoiceId = invoice.Id
        };

        // Act
        var result = await controller.Create(request);


        // Assert
        var paymentWasCreated = await db.Payments.AnyAsync();

        Assert.False(paymentWasCreated);

        var conflictObjectResult = Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenInvoiceIsAlreadyPaid_ReturnsConflict()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var controller = new PaymentsController(db);

        var invoice = new Invoice
        {
            Amount = 99.90m,
            DueDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            ),
            Status = InvoiceStatus.Paid,
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
            Invoice = invoice,
            Amount = invoice.Amount
        };

        db.Invoices.Add(invoice);
        db.Payments.Add(existingPayment);

        await db.SaveChangesAsync();

        var request = new CreatePaymentDto
        {
            InvoiceId = invoice.Id
        };

        // Act
        var result = await controller.Create(request);


        // Assert
        var newPaymentCreated = await db.Payments.AnyAsync(x => x.InvoiceId == invoice.Id && x.Id != existingPayment.Id);

        Assert.False(newPaymentCreated);

        var conflictObjectResult = Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenInvoiceIsOverdue_CreatesPayment()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var controller = new PaymentsController(db);

        var invoice = new Invoice
        {
            Amount = 99.90m,
            DueDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(-7)
            ),
            Status = InvoiceStatus.Overdue,
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

        db.ChangeTracker.Clear();

        var request = new CreatePaymentDto
        {
            InvoiceId = invoice.Id
        };

        var result = await controller.Create(request);

        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        db.ChangeTracker.Clear();

        var savedInvoice = await db.Invoices
            .SingleAsync(x => x.Id == invoice.Id);

        var savedPayment = await db.Payments
            .SingleAsync(x => x.InvoiceId == invoice.Id);

        Assert.Equal(InvoiceStatus.Paid, savedInvoice.Status);
        Assert.Equal(invoice.Amount, savedPayment.Amount);
    }

}