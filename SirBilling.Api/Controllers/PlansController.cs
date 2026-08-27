using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PlansController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _db.Plans.AsNoTracking().ToListAsync();

        var plansResponse = plans.Select(x => new PlanResponseDto
        {
            Id = x.Id,
            Name = x.Name,
            Price = x.Price,
            IsActive = x.IsActive
        }).ToList();

        return Ok(plansResponse);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound(new
            {
                message = "Plan not found."
            });
        }

        var planResponse = new PlanResponseDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Price = plan.Price,
            IsActive = plan.IsActive
        };

        return Ok(planResponse);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePlanDto data)
    {
        if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Length < 3)
        {
            return BadRequest(new { message = "The name must have at least 3 characters" });
        }

        if (data.Price <= 0)
        {
            return BadRequest(new { message = "The price must be greater than zero" });
        }

        var plan = new Plan
        {
            Name = data.Name,
            Price = data.Price
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();

        var planResponse = new PlanResponseDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Price = plan.Price,
            IsActive = plan.IsActive
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = plan.Id },
            planResponse
        );
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePlanDto data)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound(new
            {
                message = "Plan not found."
            });
        }

        if (data.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Length < 3)
            {
                return BadRequest(new
                {
                    message = "The name must have at least 3 characters"
                });
            }
            plan.Name = data.Name;
        }

        if (data.Price.HasValue)
        {
            if (data.Price <= 0)
            {
                return BadRequest(new { message = "The price must be greater than zero" });
            }

            plan.Price = data.Price.Value;

        }

        if (data.IsActive.HasValue)
        {
            plan.IsActive = data.IsActive.Value;
        }

        var planResponse = new PlanResponseDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Price = plan.Price,
            IsActive = plan.IsActive
        };

        await _db.SaveChangesAsync();

        return Ok(planResponse);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _db.Plans.Include(x => x.Subscriptions).FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound(new
            {
                message = "Plan not found."
            });
        }

        if (plan.Subscriptions.Any(x => x.Status == SubscriptionStatus.Active))
        {
            return Conflict(new
            {
                message = "Cannot delete a plan with active subscriptions."
            });
        }

        plan.IsActive = false;
        plan.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
