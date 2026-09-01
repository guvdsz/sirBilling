using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PaymentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _db.Payments.AsNoTracking().ToListAsync();

        var result = payments.Select(payment => new PaymentResponseDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (payment == null)
        {
            return NotFound(new
            {
                message = "Payment not found."
            });
        }

        var response = new PaymentResponseDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentDto data)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == data.InvoiceId);

        if (invoice == null)
        {
            return NotFound(new
            {
                message = "Invoice not found."
            });
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Conflict(new
            {
                message = "Invoice is already paid."
            });
        }

        if (invoice.Status == InvoiceStatus.Canceled)
        {
            return Conflict(new
            {
                message = "Invoice is canceled."
            });
        }

        var payment = new Payment
        {
            InvoiceId = data.InvoiceId,
            Amount = invoice.Amount
        };

        invoice.MarkAsPaid();

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var response = new PaymentResponseDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = payment.Id },
            response
        );
    }
}