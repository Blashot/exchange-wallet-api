# Exchange Wallet API

[![CI](https://github.com/Blashot/exchange-wallet-api/actions/workflows/build.yml/badge.svg)](https://github.com/Blashot/exchange-wallet-api/actions/workflows/build.yml)

A .NET Web API that imports NBP Table B exchange rates and provides a wallet system supporting deposits, withdrawals, and currency conversion.

---

## What it does

* **Exchange rates** – periodically fetches NBP Table B exchange rates using a Hangfire recurring job and persists them in PostgreSQL. Rates can also be triggered manually via the API.
* **Wallets** – users can create wallets, deposit funds in supported currencies, withdraw funds, and convert between currencies using PLN as the pivot currency.
* **Users & Authentication** – users can register, authenticate using JWT Bearer tokens, and access protected wallet operations.

---

## Tech stack

| Category        | Technology                        |
| --------------- | --------------------------------- |
| Runtime         | .NET 10 (LTS)                     |
| Web             | ASP.NET Core (Minimal APIs)       |
| ORM             | Entity Framework Core 10 + Npgsql |
| Database        | PostgreSQL                        |
| Background jobs | Hangfire (PostgreSQL storage)     |
| Authentication  | JWT Bearer                        |
| Logging         | Serilog + Seq                     |
| Testing         | xUnit + Shouldly + NSubstitute    |
| Infrastructure  | Docker Compose                    |

---

## Architecture

The solution follows a pragmatic Clean Architecture approach with a modular monolith design.

### Layers

* **Domain** – business logic, aggregates, value objects, domain events
* **Application** – commands, queries, handlers, validation
* **Infrastructure** – EF Core, Hangfire, NBP integration, persistence
* **Web.Api** – Minimal APIs and endpoint definitions

Feature code is organised in vertical slices:

* `Wallets`
* `ExchangeRates`
* `Users`

Architecture tests verify that dependencies follow Clean Architecture boundaries.

---

## Key technical challenges

One of the more interesting implementation challenges was handling optimistic concurrency for wallet operations.

Wallet balances and transaction history are stored as child entities of the Wallet aggregate. EF Core change tracking required additional handling to ensure the wallet concurrency token was updated whenever child entities changed, allowing concurrent modifications to be detected reliably and preventing lost updates.

---

## Business assumptions

* Currency conversion always goes through PLN as the pivot currency:

```text
source currency → PLN → target currency
```

Example:

```text
EUR → PLN → USD
```

* The **latest persisted** exchange rate table is used for conversions.
* Wallet balances can reach zero but cannot become negative.
* Exchange rates are imported periodically via Hangfire.
* Exchange rate imports are idempotent – already imported table numbers are skipped.
* All monetary values use `decimal` (never `double` or `float`).

---

## Running locally

The repository includes:
* `.env.example` – Docker Compose configuration template
* `appsettings.Development.Example.json` – local development configuration template

### Option A – Docker Compose (recommended)

Requirements: Docker Desktop.

Before starting the application, create a `.env` file from the provided template:

```bash
cp .env.example .env
```

The repository includes a `.env.example` file containing the required environment variables for local development.

Run:

```bash
docker compose up -d
```

This starts:

* PostgreSQL on `localhost:5433`
* API on `http://localhost:5000`
* Seq on `http://localhost:8081`

EF migrations are applied automatically on startup in the `Development` environment.

**Swagger UI**

```text
http://localhost:5000/swagger
```

**Hangfire Dashboard**

```text
http://localhost:5000/hangfire
```

---

### Option B – IDE / `dotnet run`

Create a local development configuration:

```bash
cp src/Web.Api/appsettings.Development.Example.json src/Web.Api/appsettings.Development.json
```

You can:

* run a local PostgreSQL instance, or
* reuse the Docker Compose PostgreSQL container and change the port to `5433`.

Migrations are applied automatically on startup in the `Development` environment.

The JWT secret in development configuration is intended for local use only. In a real environment, it should be overridden via user-secrets or environment variables.

---

## Running tests

```bash
dotnet test .\CleanArchitecture.slnx
```

Test projects:

* `Domain.UnitTests`
* `Application.UnitTests`
* `Infrastructure.IntegrationTests`
* `ArchitectureTests`

### Testing strategy

The solution contains multiple layers of automated tests:

* **Domain tests** verify business rules and aggregate behaviour.
* **Application tests** verify command and query handlers.
* **Integration tests** use Testcontainers with PostgreSQL to verify persistence, EF Core mappings, and optimistic concurrency behaviour.
* **Architecture tests** verify Clean Architecture dependency rules.

---

## API overview

### Authentication

| Method | Route             | Description      |
| ------ | ----------------- | ---------------- |
| POST   | `/users/register` | Register a user  |
| POST   | `/users/login`    | Obtain JWT token |
| GET    | `/users/{userId}` | Get user by ID   |

### Exchange rates

| Method | Route                    | Description                    |
| ------ | ------------------------ | ------------------------------ |
| POST   | `/exchange-rates/import` | Trigger manual NBP import      |
| GET    | `/exchange-rates/latest` | Get latest exchange rate table |

### Wallets

| Method | Route                    | Description        |
| ------ | ------------------------ | ------------------ |
| POST   | `/wallets`               | Create wallet      |
| GET    | `/wallets`               | List wallets       |
| GET    | `/wallets/{id}`          | Get wallet details |
| POST   | `/wallets/{id}/deposit`  | Deposit funds      |
| POST   | `/wallets/{id}/withdraw` | Withdraw funds     |
| POST   | `/wallets/{id}/convert`  | Convert currencies |

Wallet and Exchange rates endpoints require a valid Bearer token.

---

## Build

GitHub Actions automatically builds the solution and runs all tests on push and pull requests.

---

## Technical decisions

* **No MediatR** – handlers implement generic command/query interfaces directly and are decorated using Scrutor (validation + logging).
* **Result pattern over exceptions** – business failures return typed results instead of throwing exceptions.
* **PLN pivot conversion** – simplifies exchange rate handling and avoids inconsistencies between currency pairs.
* **Hangfire for scheduling** – used for recurring NBP exchange rate imports with dashboard visibility.
* **Idempotent imports** – repeated imports are safe and skip already persisted exchange rate tables.
* **Optimistic concurrency** – wallet operations use a version token to prevent lost updates during concurrent modifications.
* **Architecture tests** – verify layer boundaries and dependency rules.

---


## Future improvements

* **API end-to-end tests** – verify HTTP endpoints, authentication flows, and serialization using `WebApplicationFactory`.
* **Concurrency stress testing** – validate wallet behaviour under high levels of concurrent requests.
* **Outbox Pattern** – guarantee reliable domain event delivery when introducing asynchronous integrations.
* **Caching** – cache the latest exchange rate table to reduce database reads.
* **Authorization policies** – extend ownership checks with role-based access control if additional user roles are introduced.
* **Observability** – add metrics and distributed tracing using OpenTelemetry.

---