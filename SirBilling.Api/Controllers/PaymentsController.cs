using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PaymentService _paymentService;

    public PaymentsController(
        ApplicationDbContext db,
        PaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
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
        var result = await _paymentService.CreateAsync(data.InvoiceId);

        if (result.Status == CreatePaymentStatus.InvoiceNotFound)
        {
            return NotFound(new
            {
                message = "Invoice not found."
            });
        }

        if (result.Status == CreatePaymentStatus.InvoiceAlreadyPaid)
        {
            return Conflict(new
            {
                message = "Invoice is already paid."
            });
        }

        if (result.Status == CreatePaymentStatus.InvoiceCanceled)
        {
            return Conflict(new
            {
                message = "Invoice is canceled."
            });
        }

        var payment = result.Payment
        ?? throw new InvalidOperationException(
            "A successful payment result must contain a payment."
        );

        var response = new PaymentResponseDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response
        );
    }
}