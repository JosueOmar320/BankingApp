# BankingApp: .NET 8 Banking API

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core_9-SQLite-003B57?logo=sqlite&logoColor=white)
![Tests](https://img.shields.io/badge/tests-NUnit_%2B_Moq-25A162)
![Swagger](https://img.shields.io/badge/docs-Swagger-85EA2D?logo=swagger&logoColor=black)
[![CI](https://github.com/JosueOmar320/BankingApp/actions/workflows/ci.yml/badge.svg)](https://github.com/JosueOmar320/BankingApp/actions/workflows/ci.yml)

A **Banking API** built with **.NET 8** and **ASP.NET Core Web API**. It manages customers, bank accounts and a
transaction ledger (deposits, withdrawals and interest), following Clean Architecture with a rich domain model.

The SQLite database is created and migrated automatically on start-up, and in `Development` it is seeded with two
sample accounts, so you only need to run the API.

---

## Features

- Register customers and open bank accounts with an opening deposit
- Check account details and balance
- Deposits, withdrawals and interest application
- Transaction history with totals and final balance
- Business rules enforced in the domain model:
  - opening deposit of at least **10.00**
  - amounts must be positive with at most **two decimals**
  - a withdrawal can never exceed the available balance
  - the **interest rate is fixed at 10%** for every account (rounded to cents)
  - one customer per identification document
- Every balance change and its ledger entry are saved **atomically**, and accounts carry an **optimistic concurrency
  token** so two simultaneous withdrawals can never overdraw an account (the loser gets `409 Conflict` and can retry)
- Consistent error contract: every error is an [RFC 7807 problem details](https://www.rfc-editor.org/rfc/rfc7807) response
- Swagger UI with XML documentation, `/health` endpoint, structured logging with source-generated log messages
- Unit tests (domain, services), repository tests on real SQLite and HTTP integration tests with `WebApplicationFactory`
- GitHub Actions CI: format check, build with warnings as errors, tests and NuGet vulnerability audit

---

## Architecture

The solution follows **Clean Architecture**, with dependencies pointing inward:

| Project                  | Responsibility                                                                                   |
| ------------------------ | ------------------------------------------------------------------------------------------------ |
| `Banking.Domain`         | Entities with behaviour (`BankAccount` is the aggregate that owns the ledger), rules, domain exceptions, repository interfaces. No framework dependencies. |
| `Banking.Application`    | Use cases (services), request/response DTOs, `NotFoundException`                                 |
| `Banking.Infrastructure` | EF Core `DbContext`, Fluent API configurations, SQLite migrations, repositories, start-up migration and dev seeding |
| `Banking.API`            | Controllers, global exception handler (problem details), Swagger, composition root               |
| `Banking.Tests`          | Domain, service, repository and HTTP integration tests                                           |

Key design decisions:

- **The aggregate protects the invariants.** `BankAccount.Deposit/Withdraw/ApplyInterest` mutate the balance, record
  the `Transaction` and bump the `Version` token. Repositories persist the aggregate in a single `SaveChanges`, so the
  balance and the ledger cannot drift apart.
- **Errors are exceptions with a stable HTTP mapping.** `DomainException` → `400`, `NotFoundException` → `404`,
  `DuplicateDocumentNumberException` / `ConcurrencyConflictException` → `409`, anything else → `500` without leaking
  internals. Controllers stay free of `try/catch`.
- **Persistence concerns stay in Infrastructure.** Entities carry no data annotations; mapping, indexes, value
  conversions and the concurrency token live in `IEntityTypeConfiguration` classes.

## Endpoints

| Method | Route                                      | Body                              | Description                                   |
| ------ | ------------------------------------------ | --------------------------------- | --------------------------------------------- |
| `POST` | `/api/Customer`                            | `CreateCustomerDto`               | Register a customer (`201` + `Location`)      |
| `GET`  | `/api/Customer/{customerId}`               |                                   | Get a customer                                |
| `POST` | `/api/BankAccount`                         | `{ customerId, initialDeposit }`  | Open an account (`201` + `Location`)          |
| `GET`  | `/api/BankAccount/{accountNumber}`         |                                   | Get an account                                |
| `GET`  | `/api/BankAccount/{accountNumber}/balance` |                                   | Get the balance                               |
| `POST` | `/api/Transaction/Deposit`                 | `{ accountNumber, amount }`       | Deposit                                       |
| `POST` | `/api/Transaction/Withdrawal`              | `{ accountNumber, amount }`       | Withdraw                                      |
| `POST` | `/api/Transaction/Apply-Interest`          | `{ accountNumber }`               | Credit 10% interest on the balance            |
| `GET`  | `/api/Transaction/{accountNumber}/summary` |                                   | Ledger with totals and final balance          |
| `GET`  | `/health`                                  |                                   | Liveness check                                |

Enums travel as strings (`"gender": "F"`, `"transactionType": "Deposit"`). Full schemas are in Swagger, and
[`Banking.API/Banking.API.http`](Banking.API/Banking.API.http) contains ready-to-run requests.

### Error responses

All errors use `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Insufficient funds",
  "status": 400,
  "detail": "Insufficient funds: the available balance is 20.00 and 20.01 was requested.",
  "instance": "/api/Transaction/Withdrawal"
}
```

| Status | When                                                                                   |
| ------ | -------------------------------------------------------------------------------------- |
| `400`  | Invalid payload (`ValidationProblemDetails` with an `errors` map) or business rule violated |
| `404`  | Customer or account does not exist                                                     |
| `409`  | Duplicate document number, or the account changed concurrently (retry the request)    |
| `500`  | Unexpected error (details are logged, never returned)                                  |

### Sample data (Development only)

When `Database:SeedSampleData` is `true` (it is in `appsettings.Development.json`) and the database has no customers,
two accounts are created so you can try the API immediately:

| Account number | Customer        | Ledger                                              |
| -------------- | --------------- | --------------------------------------------------- |
| `1000000001`   | Ana Martínez    | open 500, deposit 250, withdraw 100, interest 65    |
| `1000000002`   | Luis Hernández  | open 1000, withdraw 200                             |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or newer (`global.json` rolls forward to the latest major)
- Visual Studio, Visual Studio Code, Rider, or any .NET compatible editor

## Running the API

1. Clone the repository:

   ```bash
   git clone https://github.com/JosueOmar320/BankingApp.git
   cd BankingApp
   ```

2. Run the API (restores, builds, migrates the database and seeds sample data):

   ```bash
   dotnet run --project Banking.API
   ```

   The API listens on:

   ```text
   https://localhost:7201
   http://localhost:5200
   ```

3. Open Swagger:

   ```text
   https://localhost:7201/swagger
   http://localhost:5200/swagger
   ```

### Configuration

| Key                                  | Default                                              | Purpose                                      |
| ------------------------------------ | ---------------------------------------------------- | -------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | `Data Source=../Banking.Infrastructure/Data/banking.db` | SQLite file; relative paths resolve against the API content root |
| `Database:SeedSampleData`            | `false` (`true` in Development)                      | Insert the sample accounts when the DB is empty |

Pending migrations are applied automatically on start-up. To add a migration after changing the model:

```bash
dotnet ef migrations add <Name> --project Banking.Infrastructure --startup-project Banking.API
```

---

## Tests

- **Project:** `Banking.Tests`
- **Frameworks:** NUnit, Moq, EF Core SQLite (in-memory), `Microsoft.AspNetCore.Mvc.Testing`

| Layer          | What is covered                                                                                          |
| -------------- | -------------------------------------------------------------------------------------------------------- |
| Domain         | Opening deposit rules, amount validation, insufficient funds, interest rounding, version bumps           |
| Application    | Services with mocked repositories: mapping, not-found, retry of account-number generation, summaries     |
| Infrastructure | Real SQLite: atomic save of balance + ledger, unique document/account numbers, **concurrency conflict**, migrations in sync with the model |
| API            | In-process HTTP tests: status codes, `Location` headers, problem details, validation, **parallel withdrawals never overdraw** |

```bash
dotnet test
```

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every push and pull request: `dotnet format` check,
build with `TreatWarningsAsErrors`, the full test suite and a NuGet vulnerability audit.
