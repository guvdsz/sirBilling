[![.NET](https://github.com/guvdsz/sirBilling/actions/workflows/dotnet.yml/badge.svg)](https://github.com/guvdsz/sirBilling/actions/workflows/dotnet.yml)

# SirBilling

[badge da CI]

Breve descrição do sistema.

## Domain flow

Customer → Subscription → Invoice → Payment

## Business rules

- Soft delete de Customer e Plan
- Apenas subscriptions ativas geram invoices
- Invoice preserva o preço do plano
- Invoice paga não pode ser cancelada
- Uma invoice possui no máximo um payment
- Payment altera a invoice para Paid

## Technologies

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- SQLite para testes
- xUnit
- WebApplicationFactory
- GitHub Actions

## API endpoints

Tabela com método, rota e descrição.

## Running locally

Pré-requisitos, conexão PostgreSQL, migrations e execução.

## Tests

Como executar os 38 testes e quais níveis são cobertos.

## Continuous integration

Explicação da CI e da proteção da main.

## Future improvements

Autenticação, Docker, observabilidade etc.
