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

        return Ok(plans);
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

        return Ok(plan);
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

        return CreatedAtAction(
            nameof(GetById),
            new { id = plan.Id },
            plan
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

        await _db.SaveChangesAsync();

        return Ok(plan);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound(new
            {
                message = "Plan not found."
            });
        }

        _db.Plans.Remove(plan);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
