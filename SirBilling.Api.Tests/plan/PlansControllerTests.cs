using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SirBilling.Api.Controllers;

namespace SirBilling.Api.Tests;

public class PlansControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsPlans()
    {
        await using var database = await TestDatabase.CreateAsync();
        var plan = new Plan { Name = "Pro", Price = 99m };
        database.Context.Plans.Add(plan);
        await database.Context.SaveChangesAsync();

        var result = await new PlansController(database.Context).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PlanResponseDto>>(ok.Value);
        Assert.Equal(plan.Id, Assert.Single(response).Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await new PlansController(database.Context).GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenInvalidPrice_ReturnsBadRequest()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await new PlansController(database.Context).Create(
            new CreatePlanDto { Name = "Pro", Price = 0m });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await database.Context.Plans.ToListAsync());
    }

    [Fact]
    public async Task CreateAndUpdate_WhenValid_PersistsPlan()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = new PlansController(database.Context);

        var createResult = await controller.Create(new CreatePlanDto { Name = "Pro", Price = 99m });
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var createdPlan = Assert.IsType<PlanResponseDto>(created.Value);

        var updateResult = await controller.Update(createdPlan.Id,
            new UpdatePlanDto { Name = "Business", Price = 149m, IsActive = false });

        var updated = Assert.IsType<OkObjectResult>(updateResult);
        var response = Assert.IsType<PlanResponseDto>(updated.Value);
        Assert.Equal("Business", response.Name);
        Assert.Equal(149m, response.Price);
        Assert.False(response.IsActive);
        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.Plans.SingleAsync(x => x.Id == createdPlan.Id);
        Assert.Equal("Business", saved.Name);
        Assert.Equal(149m, saved.Price);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task Delete_WhenActiveSubscriptionExists_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var plan = new Plan { Name = "Pro", Price = 99m };
        var customer = new Customer { Name = "Ana Silva", Email = "ana@example.com" };
        database.Context.Add(new Subscription { Customer = customer, Plan = plan });
        await database.Context.SaveChangesAsync();

        var result = await new PlansController(database.Context).Delete(plan.Id);

        Assert.IsType<ConflictObjectResult>(result);
        database.Context.ChangeTracker.Clear();
        Assert.True((await database.Context.Plans.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Delete_WhenNoActiveSubscription_DeactivatesPlan()
    {
        await using var database = await TestDatabase.CreateAsync();
        var plan = new Plan { Name = "Pro", Price = 99m };
        database.Context.Plans.Add(plan);
        await database.Context.SaveChangesAsync();

        var result = await new PlansController(database.Context).Delete(plan.Id);

        Assert.IsType<NoContentResult>(result);
        var saved = await database.Context.Plans
            .IgnoreQueryFilters()
            .SingleAsync();
        Assert.False(saved.IsActive);
        Assert.NotNull(saved.DeletedAt);
    }
}
