[![.NET](https://github.com/guvdsz/sirBilling/actions/workflows/dotnet.yml/badge.svg)](https://github.com/guvdsz/sirBilling/actions/workflows/dotnet.yml)

# SirBilling

SirBilling is a billing API built with ASP.NET Core and Entity Framework Core. It manages customers, plans, subscriptions, invoices and payments while enforcing billing lifecycle rules.

## Domain flow

```text
Customer -> Subscription -> Invoice -> Payment
```

Customers subscribe to active plans. Invoices are generated from the current plan price, and payments settle pending or overdue invoices.

## Business rules

- Customers and plans are soft-deleted.
- Only active subscriptions can generate invoices.
- An invoice stores the plan price at creation time.
- Paid invoices cannot be canceled.
- An invoice can have at most one payment.
- Creating a payment marks its invoice as `Paid`.
- A customer cannot be deleted while an active subscription exists.
- A plan cannot be deleted while an active subscription exists.
- A customer cannot have more than one active subscription for the same plan.

## Technology stack

- .NET 10 and ASP.NET Core Web API
- Entity Framework Core with PostgreSQL
- SQLite in-memory databases for controller tests
- xUnit
- GitHub Actions

## API endpoints

| Method   | Route                            | Description                                      |
| -------- | -------------------------------- | ------------------------------------------------ |
| `GET`    | `/api/health`                    | Returns the API health status.                   |
| `GET`    | `/api/customers`                 | Lists customers.                                 |
| `GET`    | `/api/customers/{id}`            | Gets a customer by ID.                           |
| `POST`   | `/api/customers`                 | Creates a customer.                              |
| `PATCH`  | `/api/customers/{id}`            | Partially updates a customer.                    |
| `DELETE` | `/api/customers/{id}`            | Soft-deletes a customer.                         |
| `GET`    | `/api/plans`                     | Lists plans.                                     |
| `GET`    | `/api/plans/{id}`                | Gets a plan by ID.                               |
| `POST`   | `/api/plans`                     | Creates a plan.                                  |
| `PATCH`  | `/api/plans/{id}`                | Partially updates a plan.                        |
| `DELETE` | `/api/plans/{id}`                | Deactivates and soft-deletes a plan.             |
| `GET`    | `/api/subscriptions`             | Lists subscriptions.                             |
| `GET`    | `/api/subscriptions/{id}`        | Gets a subscription by ID.                       |
| `POST`   | `/api/subscriptions`             | Creates a subscription.                          |
| `POST`   | `/api/subscriptions/{id}/cancel` | Cancels an active subscription.                  |
| `GET`    | `/api/invoices`                  | Lists invoices.                                  |
| `GET`    | `/api/invoices/{id}`             | Gets an invoice by ID.                           |
| `POST`   | `/api/invoices`                  | Creates an invoice for an active subscription.   |
| `POST`   | `/api/invoices/{id}/cancel`      | Cancels an unpaid invoice.                       |
| `GET`    | `/api/payments`                  | Lists payments.                                  |
| `GET`    | `/api/payments/{id}`             | Gets a payment by ID.                            |
| `POST`   | `/api/payments`                  | Creates a payment and marks the invoice as paid. |

## Running locally

### Prerequisites

- .NET SDK 10.0.400 or compatible
- PostgreSQL running locally
- The `dotnet-ef` tool installed if you need to apply migrations:

```bash
dotnet tool install --global dotnet-ef
```

The development connection string is in `SirBilling.Api/appsettings.json`. Update it for your local PostgreSQL credentials when necessary.

Apply the existing migrations from the repository root:

```bash
dotnet ef database update \
	--project SirBilling.Api/SirBilling.Api.csproj \
	--startup-project SirBilling.Api/SirBilling.Api.csproj
```

Start the API with:

```bash
dotnet run --project SirBilling.Api/SirBilling.Api.csproj
```

In Development, the OpenAPI document is available at `/openapi/v1.json`. HTTPS redirection is enabled by default; use the URL printed by `dotnet run`.

## Tests

Run all tests from the repository root:

```bash
dotnet test SirBilling.slnx
```

The test project uses SQLite in-memory databases and calls controllers directly. The suite covers successful and invalid requests, not-found and conflict responses, soft deletion, subscription lifecycle transitions, invoice lifecycle rules, payment creation and invoice status changes.

## Continuous integration

GitHub Actions runs on pushes to `main`, pull requests targeting `main`, and manual dispatches. The workflow restores and audits NuGet dependencies, builds the solution in Release mode with warnings treated as errors, and runs the test suite.

Changes should be merged into `main` through a pull request after the checks pass.

## Future improvements

- Authentication and authorization
- Docker and containerized local development
- Pagination and filtering for collection endpoints
- Structured logging, metrics and distributed tracing
- Integration tests for the HTTP pipeline
