using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirBilling.Api.Controllers;

namespace SirBilling.Api.Tests;

public class SubscriptionsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsSubscriptions()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        var plan = new Plan { Name = "Pro", Price = 99m };
        var subscription = new Subscription { Customer = customer, Plan = plan };
        database.Context.Subscriptions.Add(subscription);
        await database.Context.SaveChangesAsync();

        var result = await new SubscriptionsController(database.Context).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<SubscriptionResponseDto>>(ok.Value);
        Assert.Equal(subscription.Id, Assert.Single(response).Id);
    }

    [Fact]
    public async Task Create_WhenCustomerIsMissing_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = new SubscriptionsController(database.Context);
        var data = new CreateSubscriptionDto { CustomerId = Guid.NewGuid(), PlanId = Guid.NewGuid() };

        var result = await controller.Create(data);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenPlanIsMissing_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        database.Context.Customers.Add(customer);
        await database.Context.SaveChangesAsync();

        var result = await new SubscriptionsController(database.Context).Create(
            new CreateSubscriptionDto { CustomerId = customer.Id, PlanId = Guid.NewGuid() });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenPlanIsInactive_ReturnsBadRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        var plan = new Plan { Name = "Pro", Price = 99m, IsActive = false };
        database.Context.AddRange(customer, plan);
        await database.Context.SaveChangesAsync();

        var result = await new SubscriptionsController(database.Context).Create(
            new CreateSubscriptionDto { CustomerId = customer.Id, PlanId = plan.Id });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedSubscription()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        var plan = new Plan { Name = "Pro", Price = 99m };
        database.Context.AddRange(customer, plan);
        await database.Context.SaveChangesAsync();

        var result = await new SubscriptionsController(database.Context).Create(
            new CreateSubscriptionDto { CustomerId = customer.Id, PlanId = plan.Id });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<SubscriptionResponseDto>(created.Value);
        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal(SubscriptionStatus.Active, response.Status);
    }

    [Fact]
    public async Task Create_WhenActiveSubscriptionAlreadyExists_ReturnsBadRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        var plan = new Plan { Name = "Pro", Price = 99m };
        database.Context.Add(new Subscription { Customer = customer, Plan = plan });
        await database.Context.SaveChangesAsync();

        var result = await new SubscriptionsController(database.Context).Create(
            new CreateSubscriptionDto { CustomerId = customer.Id, PlanId = plan.Id });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(await database.Context.Subscriptions.ToListAsync());
    }

    [Fact]
    public async Task Cancel_WhenActive_SetsCanceledStatusAndDate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        var plan = new Plan { Name = "Pro", Price = 99m };
        var subscription = new Subscription { Customer = customer, Plan = plan };
        database.Context.Subscriptions.Add(subscription);
        await database.Context.SaveChangesAsync();

        var result = await new SubscriptionsController(database.Context).Cancel(subscription.Id);

        Assert.IsType<NoContentResult>(result);
        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.Subscriptions
            .SingleAsync(x => x.Id == subscription.Id);
        Assert.Equal(SubscriptionStatus.Canceled, saved.Status);
        Assert.NotNull(saved.CanceledAt);
    }
}
