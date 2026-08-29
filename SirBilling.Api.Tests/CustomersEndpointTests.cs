using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

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

    [Theory]
    [InlineData("A", null, "Name")]
    [InlineData(null, "invalid-email", "Email")]
    public async Task Update_WhenDataIsInvalid_ReturnsValidationProblem(
    string? name,
    string? email,
    string expectedField)
    {
        await _factory.ResetDatabaseAsync();

        var createRequest = new CreateCustomerDto
        {
            Name = "Sophia",
            Email = "test@email.com"
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            createRequest
        );

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);


        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponseDto>();

        Assert.NotNull(createdCustomer);

        var request = new UpdateCustomerDto
        {
            Name = name,
            Email = email
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/customers/{createdCustomer.Id}",
            request
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Single(problem.Errors);
        Assert.Contains(expectedField, problem.Errors.Keys);
    }


    [Theory]
    [InlineData("A", "ana@example.com", "Name")]
    [InlineData("Ana Silva", "invalid-email", "Email")]
    public async Task Create_WhenDataIsInvalid_ReturnsValidationProblem(
    string name,
    string email,
    string expectedField)
    {
        await _factory.ResetDatabaseAsync();

        var request = new CreateCustomerDto
        {
            Name = name,
            Email = email
        };

        var response = await _client.PostAsJsonAsync(
            "/api/customers",
            request
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains(expectedField, problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_WhenNameExceedsMaximumLength_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        var request = new CreateCustomerDto
        {
            Name = new string('A', 101),
            Email = "ana@example.com"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/customers",
            request
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("Name", problem.Errors.Keys);
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

