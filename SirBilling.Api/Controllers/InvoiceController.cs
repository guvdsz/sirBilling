using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public InvoiceController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _db.Invoices.AsNoTracking().ToListAsync();

        var response = invoices.Select(invoice => new InvoiceResponseDto
        {
            Id = invoice.Id,
            SubscriptionId = invoice.SubscriptionId,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt
        });


        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var invoice = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound(new
            {
                message = "Invoice not found."
            });
        }

        var response = new InvoiceResponseDto
        {
            Id = invoice.Id,
            SubscriptionId = invoice.SubscriptionId,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceDto data)
    {
        var subscription = await _db.Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(x => x.Id == data.SubscriptionId);

        if (subscription == null)
        {
            return NotFound(new
            {
                message = "Subscription not found."
            });
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            return BadRequest(new
            {
                message = "Cannot create invoice for inactive subscription."
            });
        }

        if (data.DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Due date must be in the future."
            });
        }

        var invoice = new Invoice
        {
            SubscriptionId = data.SubscriptionId,
            Amount = subscription.Plan.Price,
            DueDate = data.DueDate,
            Status = InvoiceStatus.Pending
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var response = new InvoiceResponseDto
        {
            Id = invoice.Id,
            SubscriptionId = invoice.SubscriptionId,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate,
            CreatedAt = invoice.CreatedAt,
            Status = invoice.Status
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = invoice.Id },
            response
        );
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound(new
            {
                message = "Invoice not found."
            });
        }

        invoice.Status = InvoiceStatus.Canceled;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}