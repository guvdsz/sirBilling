using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SubscriptionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subscriptions = await _db.Subscriptions.AsNoTracking().ToListAsync();

        var response = subscriptions.Select(subscription => new SubscriptionResponseDto
        {
            Id = subscription.Id,
            CustomerId = subscription.CustomerId,
            PlanId = subscription.PlanId,
            Status = subscription.Status,
            CanceledAt = subscription.CanceledAt
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var subscription = await _db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (subscription == null)
        {
            return NotFound(new
            {
                message = "Subscription not found."
            });
        }

        var response = new SubscriptionResponseDto
        {
            Id = subscription.Id,
            CustomerId = subscription.CustomerId,
            PlanId = subscription.PlanId,
            Status = subscription.Status,
            CanceledAt = subscription.CanceledAt
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSubscriptionDto data)
    {
        if (!await _db.Customers.AnyAsync(x => x.Id == data.CustomerId))
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        if (!await _db.Plans.AnyAsync(x => x.Id == data.PlanId))
        {
            return NotFound(new
            {
                message = "Plan not found."
            });
        }

        if (!await _db.Plans.AnyAsync(x => x.Id == data.PlanId && x.IsActive))
        {
            return BadRequest(new
            {
                message = "The plan is not active."
            });
        }

        if (await _db.Subscriptions.AnyAsync(x => x.CustomerId == data.CustomerId && x.PlanId == data.PlanId && x.Status == SubscriptionStatus.Active))
        {
            return BadRequest(new
            {
                message = "The customer already has an active subscription for this plan."
            });
        }

        var subscription = new Subscription
        {
            CustomerId = data.CustomerId,
            PlanId = data.PlanId
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        var response = new SubscriptionResponseDto
        {
            Id = subscription.Id,
            CustomerId = subscription.CustomerId,
            PlanId = subscription.PlanId,
            Status = subscription.Status,
            CanceledAt = subscription.CanceledAt
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = subscription.Id },
            response
        );
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(x => x.Id == id);

        if (subscription == null)
        {
            return NotFound(new
            {
                message = "Subscription not found."
            });
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            return Conflict(new
            {
                message = "Only active subscriptions can be canceled."
            });
        }

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}