using Microsoft.EntityFrameworkCore;

namespace SirBilling.Api.Tests;

public class CustomerPersistenceTests
{
    [Fact]
    public async Task SaveChanges_WhenCustomerEmailsAreDuplicated_Throws()
    {
        await using var database = await TestDatabase.CreateAsync();

        database.Context.Customers.AddRange(
            new Customer
            {
                Name = "Ana",
                Email = "a@example.com"
            },
            new Customer
            {
                Name = "Maria",
                Email = "a@example.com"
            }
        );

        await Assert.ThrowsAsync<DbUpdateException>(
            () => database.Context.SaveChangesAsync()
        );
    }
}