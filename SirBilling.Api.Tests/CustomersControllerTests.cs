using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirBilling.Api.Controllers;

namespace SirBilling.Api.Tests;

public class CustomersControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsCustomers()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        database.Context.Customers.Add(customer);
        await database.Context.SaveChangesAsync();

        var result = await new CustomersController(database.Context).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<CustomerResponseDto>>(ok.Value);
        var item = Assert.Single(response);
        Assert.Equal(customer.Id, item.Id);
        Assert.Equal("Ana Silva", item.Name);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await new CustomersController(database.Context).GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedCustomer()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = new CustomersController(database.Context);

        var result = await controller.Create(new CreateCustomerDto
        {
            Name = "Ana Silva",
            Email = "ana@example.com"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<CustomerResponseDto>(created.Value);
        Assert.Equal("Ana Silva", response.Name);
        Assert.True(await database.Context.Customers.AnyAsync(x => x.Id == response.Id));
    }

    [Fact]
    public async Task Create_WhenNameOrEmailIsInvalid_ReturnsBadRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = new CustomersController(database.Context);

        var shortName = await controller.Create(new CreateCustomerDto { Name = "A", Email = "a@example.com" });
        var invalidEmail = await controller.Create(new CreateCustomerDto { Name = "Ana", Email = "invalid" });

        Assert.IsType<BadRequestObjectResult>(shortName);
        Assert.IsType<BadRequestObjectResult>(invalidEmail);
        Assert.Empty(await database.Context.Customers.ToListAsync());
    }

    [Fact]
    public async Task Update_WhenValid_UpdatesOnlyProvidedFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        database.Context.Customers.Add(customer);
        await database.Context.SaveChangesAsync();

        var result = await new CustomersController(database.Context).Update(
            customer.Id, new UpdateCustomerDto { Name = "Maria Silva" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CustomerResponseDto>(ok.Value);
        Assert.Equal("Maria Silva", response.Name);
        Assert.Equal("ana@example.com", response.Email);
        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.Customers.SingleAsync(x => x.Id == customer.Id);
        Assert.Equal("Maria Silva", saved.Name);
        Assert.Equal("ana@example.com", saved.Email);
    }

    [Fact]
    public async Task Delete_WhenActiveSubscriptionExists_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        var plan = new Plan { Name = "Pro", Price = 99m };
        database.Context.Add(new Subscription { Customer = customer, Plan = plan });
        await database.Context.SaveChangesAsync();

        var result = await new CustomersController(database.Context).Delete(customer.Id);

        Assert.IsType<ConflictObjectResult>(result);
        database.Context.ChangeTracker.Clear();
        Assert.Null((await database.Context.Customers.SingleAsync()).DeletedAt);
    }

    [Fact]
    public async Task Delete_WhenNoActiveSubscription_SoftDeletesCustomer()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        database.Context.Customers.Add(customer);
        await database.Context.SaveChangesAsync();

        var result = await new CustomersController(database.Context).Delete(customer.Id);

        Assert.IsType<NoContentResult>(result);
        var saved = await database.Context.Customers
            .IgnoreQueryFilters()
            .SingleAsync();
        Assert.NotNull(saved.DeletedAt);
    }
}
