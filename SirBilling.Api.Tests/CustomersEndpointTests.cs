using System.Net;
using System.Net.Http.Json;

namespace SirBilling.Api.Tests;

public class CustomersEndpointTests :
    IClassFixture<SirBillingWebApplicationFactory>
{
    private readonly SirBillingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomersEndpointTests(
        SirBillingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedCustomer()
    {
        await _factory.ResetDatabaseAsync();

        var request = new CreateCustomerDto
        {
            Name = "Ana Silva",
            Email = "ana@example.com"
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createdCustomer = await createResponse.Content
            .ReadFromJsonAsync<CustomerResponseDto>();

        Assert.NotNull(createdCustomer);
        Assert.Equal("Ana Silva", createdCustomer.Name);
        Assert.Equal("ana@example.com", createdCustomer.Email);

        Assert.NotNull(createResponse.Headers.Location);

        var getResponse = await _client.GetAsync(
            createResponse.Headers.Location
        );

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedCustomer = await getResponse.Content
            .ReadFromJsonAsync<CustomerResponseDto>();

        Assert.NotNull(savedCustomer);
        Assert.Equal(createdCustomer.Id, savedCustomer.Id);
        Assert.Equal(createdCustomer.Email, savedCustomer.Email);
    }
}