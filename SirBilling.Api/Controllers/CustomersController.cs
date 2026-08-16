using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CustomersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _db.Customers.AsNoTracking().ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        var response = new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto data)
    {
        if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Length < 3)
        {
            return BadRequest(new { message = "The name must have at least 3 characters" });
        }

        if (data.Email is null || !data.Email.Contains("@"))
        {
            return BadRequest(new { message = "The email is not valid" });
        }

        var customer = new Customer
        {
            Name = data.Name,
            Email = data.Email
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.Id },
            customer
        );
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerDto data)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
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
            customer.Name = data.Name;
        }

        if (data.Email is not null)
        {
            var emailChecker = new EmailAddressAttribute();

            if (!emailChecker.IsValid(data.Email))
            {
                return BadRequest(new
                {
                    message = "The e-mail inserted is invalid. Please check."
                });
            }

            customer.Email = data.Email;
        }

        await _db.SaveChangesAsync();

        return Ok(customer);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        _db.Customers.Remove(customer);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
