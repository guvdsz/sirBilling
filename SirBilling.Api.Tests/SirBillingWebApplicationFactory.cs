using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace SirBilling.Api.Tests;

public sealed class SirBillingWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType ==
                    typeof(
                        IDbContextOptionsConfiguration<
                            ApplicationDbContext
                        >
                    )
            );

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            var connectionDescriptor = services.SingleOrDefault(
                service => service.ServiceType ==
                    typeof(DbConnection)
            );

            if (connectionDescriptor is not null)
            {
                services.Remove(connectionDescriptor);
            }

            services.AddSingleton<DbConnection>(_ =>
            {
                var connection =
                    new SqliteConnection("Data Source=:memory:");

                connection.Open();

                return connection;
            });

            services.AddDbContext<ApplicationDbContext>(
                (provider, options) =>
                {
                    var connection =
                        provider.GetRequiredService<DbConnection>();

                    options.UseSqlite(connection);
                }
            );
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}