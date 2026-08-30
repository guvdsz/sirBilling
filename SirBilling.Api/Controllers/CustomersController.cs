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

        var response = customers.Select(customer => new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return CustomerNotFoundProblem();
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
        var normalizedEmail = data.Email.ToLowerInvariant();

        var emailExists = await _db.Customers.IgnoreQueryFilters().AnyAsync(x => x.Email == normalizedEmail);

        if (emailExists)
        {

            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already in use",
                detail: "The e-mail inserted is already in use. Please check."
            );
        }

        var customer = new Customer
        {
            Name = data.Name,
            Email = normalizedEmail
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var response = new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.Id },
            response
        );
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerDto data)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return CustomerNotFoundProblem();
        }

        if (data.Name is not null)
        {
            customer.Name = data.Name;
        }

        if (data.Email is not null)
        {
            var normalizedEmail = data.Email.ToLowerInvariant();
            var emailExists = await _db.Customers.IgnoreQueryFilters().AnyAsync(x => x.Email == normalizedEmail && x.Id != id);

            if (emailExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Email already in use",
                    detail: "The e-mail inserted is already in use. Please check."
                );
            }

            customer.Email = normalizedEmail;
        }

        var response = new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };

        await _db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
        {
            return CustomerNotFoundProblem();
        }

        var hasActiveSubscriptions = await _db.Subscriptions
    .AnyAsync(x => x.CustomerId == id && x.Status == SubscriptionStatus.Active);

        if (hasActiveSubscriptions)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cannot delete customer with active subscriptions",
                detail: "The customer has active subscriptions and cannot be deleted."
            );
        }

        customer.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private ObjectResult CustomerNotFoundProblem()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Customer not found",
            detail: "No customer was found with the provided identifier."
        );
    }
}
