using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SirBilling.Api.Tests;

public class BillingFlowEndpointTests :
    IClassFixture<SirBillingWebApplicationFactory>
{
    private readonly SirBillingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    public BillingFlowEndpointTests(
        SirBillingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteBillingFlow_WhenValid_MarksInvoiceAsPaid()
    {
        await _factory.ResetDatabaseAsync();

        // 1. Customer
        var customerHttpResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerDto
            {
                Name = "Ana Silva",
                Email = "ana@example.com"
            }
        );

        var customer =
            await ReadCreatedAsync<CustomerResponseDto>(
                customerHttpResponse
            );

        // 2. Plan
        var planHttpResponse = await _client.PostAsJsonAsync(
            "/api/plans",
            new CreatePlanDto
            {
                Name = "Pro",
                Price = 99.90m
            }
        );

        var plan =
            await ReadCreatedAsync<PlanResponseDto>(
                planHttpResponse
            );

        // 3. Subscription
        var subscriptionHttpResponse =
            await _client.PostAsJsonAsync(
                "/api/subscriptions",
                new CreateSubscriptionDto
                {
                    CustomerId = customer.Id,
                    PlanId = plan.Id
                }
            );

        var subscription =
            await ReadCreatedAsync<SubscriptionResponseDto>(
                subscriptionHttpResponse
            );

        Assert.Equal(
            SubscriptionStatus.Active,
            subscription.Status
        );

        // 4. Invoice
        var invoiceHttpResponse = await _client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceDto
            {
                SubscriptionId = subscription.Id,
                DueDate = DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(7)
                )
            }
        );

        var invoice =
            await ReadCreatedAsync<InvoiceResponseDto>(
                invoiceHttpResponse
            );

        Assert.Equal(plan.Price, invoice.Amount);
        Assert.Equal(InvoiceStatus.Pending, invoice.Status);

        // 5. Payment
        var paymentHttpResponse = await _client.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentDto
            {
                InvoiceId = invoice.Id
            }
        );

        var payment =
            await ReadCreatedAsync<PaymentResponseDto>(
                paymentHttpResponse
            );

        Assert.Equal(invoice.Id, payment.InvoiceId);
        Assert.Equal(invoice.Amount, payment.Amount);

        var finalInvoiceHttpResponse = await _client.GetAsync(
            $"/api/invoices/{invoice.Id}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            finalInvoiceHttpResponse.StatusCode
        );

        var finalInvoice =
            await finalInvoiceHttpResponse.Content
                .ReadFromJsonAsync<InvoiceResponseDto>(
                    JsonOptions
                );

        Assert.NotNull(finalInvoice);
        Assert.Equal(InvoiceStatus.Paid, finalInvoice.Status);
    }

    private static async Task<T> ReadCreatedAsync<T>(
        HttpResponseMessage response)
        where T : class
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<T>(JsonOptions);

        return Assert.IsType<T>(content);
    }
}