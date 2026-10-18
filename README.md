# BankingApp: .NET 10 Banking API

![.NET 10](https://img.shields.io/badge/.NET-10_LTS-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core_10-SQLite-003B57?logo=sqlite&logoColor=white)
![Tests](https://img.shields.io/badge/tests-NUnit_%2B_Moq-25A162)
![Swagger](https://img.shields.io/badge/docs-Swagger-85EA2D?logo=swagger&logoColor=black)
[![CI](https://github.com/JosueOmar320/BankingApp/actions/workflows/ci.yml/badge.svg)](https://github.com/JosueOmar320/BankingApp/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/JosueOmar320/BankingApp?sort=semver)](https://github.com/JosueOmar320/BankingApp/releases)
[![Container image](https://img.shields.io/badge/ghcr.io-josueomar320%2Fbankingapp-2496ED?logo=docker&logoColor=white)](https://github.com/JosueOmar320/BankingApp/pkgs/container/bankingapp)
[![License: MIT](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)

A **Banking API** built with **.NET 10 (LTS)** and **ASP.NET Core Web API**. It manages customers, bank accounts and a
transaction ledger (deposits, withdrawals and interest), following Clean Architecture with a rich domain model.
Customers authenticate with a JWT and can only operate their own accounts.

No database file is checked in: the SQLite database is created and migrated automatically on start-up, and in
`Development` it is seeded with two sample accounts (password `Banking123!`), so you only need to run the API
(locally or with Docker).

---

## Features

- Register customers (public sign-up with a password) and log in to obtain a **JWT bearer token**
- **Authorization per account holder**: every account operation is limited to the authenticated owner (`403` otherwise)
- Open bank accounts with an opening deposit
- Check account details and balance
- Deposits, withdrawals and interest application
- **Transfers** between accounts, even across customers, recorded on both ledgers in one transaction
- Paginated transaction history with totals and final balance
- Business rules enforced in the domain model:
  - opening deposit of at least **10.00**
  - amounts must be positive with at most **two decimals**
  - a withdrawal or transfer can never exceed the available balance, and a transfer needs two different accounts
  - the **interest rate is fixed at 10%** for every account (rounded to cents)
  - one customer per identification document
- Every balance change and its ledger entry are saved **atomically**, and accounts carry an **optimistic concurrency
  token** so two simultaneous withdrawals can never overdraw an account (the loser gets `409 Conflict` and can retry)
- Passwords stored only as **PBKDF2 hashes** (ASP.NET Core Identity's hasher); login never reveals which credential was wrong
- Consistent error contract: every error is an [RFC 7807 problem details](https://www.rfc-editor.org/rfc/rfc7807) response, including `401`/`403`/`429`, with the request's correlation id
- **Rate limits** on login, registration and money operations (`429` + `Retry-After`), and an `X-Correlation-Id` on every response
- Error messages in **English or Spanish** (`Accept-Language`), each with a stable `code` so clients never parse text
- Swagger UI with XML documentation and an **Authorize** button for the bearer token, `/health` endpoint, structured logging with source-generated log messages
- Unit tests (domain, services), repository tests on real SQLite and HTTP integration tests with `WebApplicationFactory`
- GitHub Actions CI: format check, build with warnings as errors, tests and NuGet vulnerability audit
- Multi-stage `Dockerfile` (non-root runtime image) and `docker-compose.yml` with a persistent volume for the database
- Versioned releases: a `v*.*.*` tag publishes the image to GitHub Container Registry and creates the GitHub Release from the [CHANGELOG](CHANGELOG.md)

---

## Architecture

The solution follows **Clean Architecture**, with dependencies pointing inward:

| Project                  | Responsibility                                                                                   |
| ------------------------ | ------------------------------------------------------------------------------------------------ |
| `Banking.Domain`         | Entities with behaviour (`BankAccount` is the aggregate that owns the ledger, `Customer` owns the password policy and hash), rules, domain exceptions with stable error codes, repository and `IPasswordHasher` interfaces. No framework dependencies. |
| `Banking.Application`    | Use cases (services, including login and the ownership checks), request/response DTOs with localized validation messages, `NotFoundException`, `ForbiddenException`, `InvalidCredentialsException`, `ICurrentCustomer` / `IAccessTokenIssuer` ports |
| `Banking.Infrastructure` | EF Core `DbContext`, Fluent API configurations, SQLite migrations, repositories, PBKDF2 password hasher, start-up migration and dev seeding |
| `Banking.API`            | Controllers, JWT bearer authentication (token issuer, `sub` claim → current customer), global exception handler (problem details), localization (`Accept-Language`, `.resx`), Swagger, composition root |
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
- **The domain speaks in codes, the API speaks in languages.** Domain and application exceptions implement
  `IHasErrorCode`: a stable code (`InsufficientFunds`, `DepositBelowMinimum`, `CustomerNotFound`…) plus the values the
  message needs. Only `GlobalExceptionHandler` turns a code into text, through `IStringLocalizer` and `.resx` files, so
  the inner layers never depend on localization.
- **Authorization is a use-case rule, not a controller detail.** The API only establishes *who* is calling
  (`ICurrentCustomer`, read from the token's `sub` claim); each service loads the entity and compares its owner with the
  caller, so a missing account is still `404` and someone else's is `403`, whichever entry point is used. The API is
  also secure by default: a fallback policy requires an authenticated customer on every endpoint unless it opts out with
  `[AllowAnonymous]` (registration, login, health).
- **Passwords never leave the domain in clear.** `Customer.Create` enforces the password policy (8–128 characters) and
  stores the hash produced by `IPasswordHasher`; the infrastructure implements it with ASP.NET Core Identity's PBKDF2
  hasher. Response DTOs never include the hash.

### Why these choices

- **SQLite instead of a database server.** The API is self-contained: clone, run, and the schema is created. There is
  nothing to install, the integration tests run against the real provider (in-memory SQLite), and because the mapping
  lives in EF Core configurations, moving to PostgreSQL or SQL Server means swapping the provider and the migrations,
  not the domain.
- **Optimistic concurrency instead of locks.** Money operations are short and conflicts are rare, so each account
  carries a `Version` token checked on every `UPDATE`. Two withdrawals that race for the same balance cannot both
  succeed: the second one gets `409 Conflict` and retries with fresh data. No pessimistic locks, no long transactions,
  and the rule is enforced by the database, not by careful code in every caller.
- **ProblemDetails for every error.** Clients get one shape for validation errors, business rules, missing resources
  and unexpected failures (RFC 7807), with a stable `code` to branch on and a localized `detail` to display. The
  mapping lives in a single `IExceptionHandler`, so controllers do not catch exceptions and the contract cannot drift
  between endpoints.
- **Clean Architecture, pragmatically.** Four projects with dependencies pointing inward keep the domain free of
  frameworks and testable in isolation, without introducing CQRS, MediatR or generic repositories the project does
  not need.

## Endpoints

| Method | Route                                      | Auth     | Body                                   | Description                                   |
| ------ | ------------------------------------------ | -------- | -------------------------------------- | --------------------------------------------- |
| `POST` | `/api/Customer`                            | public   | `CreateCustomerDto` (incl. `password`) | Register a customer (`201` + `Location`)      |
| `POST` | `/api/Auth/login`                          | public   | `{ documentNumber, password }`         | Get a JWT (`200`, `401` on bad credentials)   |
| `GET`  | `/api/Customer/{customerId}`               | own      |                                        | Get a customer                                |
| `POST` | `/api/BankAccount`                         | own      | `{ customerId, initialDeposit }`       | Open an account for yourself (`201` + `Location`) |
| `GET`  | `/api/BankAccount/{accountNumber}`         | owner    |                                        | Get an account                                |
| `GET`  | `/api/BankAccount/{accountNumber}/balance` | owner    |                                        | Get the balance                               |
| `POST` | `/api/Transaction/Deposit`                 | owner    | `{ accountNumber, amount }`            | Deposit                                       |
| `POST` | `/api/Transaction/Withdrawal`              | owner    | `{ accountNumber, amount }`            | Withdraw                                      |
| `POST` | `/api/Transaction/Apply-Interest`          | owner    | `{ accountNumber }`                    | Credit 10% interest on the balance            |
| `POST` | `/api/Transaction/Transfer`                | owner    | `{ sourceAccountNumber, destinationAccountNumber, amount }` | Move money from your account to any other account |
| `GET`  | `/api/Transaction/{accountNumber}/summary` | owner    | `?page=1&pageSize=50`                  | Ledger totals, final balance and one page of transactions |
| `GET`  | `/health`                                  | public   |                                        | Liveness check                                |

*own* / *owner*: a bearer token is required, and the customer or account must belong to the authenticated customer
(`401` without a valid token, `403` for someone else's data).

A transfer debits the caller's source account (`TransferOut`) and credits the destination (`TransferIn`) in one database
transaction; the destination may belong to another customer and only has to exist. Both accounts use optimistic
concurrency, so a transfer that races with another change to either account is rejected with `409` and can be retried.

The summary is paginated: the totals (`totalDeposits`, `totalWithdrawals`, `totalInterest`, `finalBalance`) always cover
the whole ledger, while `transactions` holds one page (oldest first) and `page` says where it sits:

```json
{
  "accountNumber": "1000000001",
  "totalDeposits": 750.00,
  "totalWithdrawals": 100.00,
  "totalInterest": 65.00,
  "totalTransfersIn": 0.00,
  "totalTransfersOut": 0.00,
  "finalBalance": 715.00,
  "transactions": [ { "transactionId": 3, "transactionType": "Withdrawal", "amount": 100.00, "balanceAfter": 650.00, "transactionDate": "…" } ],
  "page": { "number": 2, "size": 2, "totalItems": 4, "totalPages": 2 }
}
```

`page` defaults to 1 and `pageSize` to 50 (maximum 200); values out of range return a `400` validation problem.

Enums travel as strings (`"gender": "F"`, `"transactionType": "Deposit"`). Full schemas are in Swagger, and
[`Banking.API/Banking.API.http`](Banking.API/Banking.API.http) contains ready-to-run requests.

### Authentication

1. **Register** (public) with `POST /api/Customer`; the payload includes a `password` of 8–128 characters.
2. **Log in** with `POST /api/Auth/login` using the document number and password:

   ```bash
   curl -s -X POST http://localhost:5200/api/Auth/login \
     -H "Content-Type: application/json" \
     -d '{ "documentNumber": "001-120490-0001A", "password": "Banking123!" }'
   ```

   ```json
   {
     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "tokenType": "Bearer",
     "expiresAt": "2026-10-18T07:12:45Z",
     "customerId": 1
   }
   ```

3. **Send the token** on every other request:

   ```bash
   curl -s http://localhost:5200/api/BankAccount/1000000001/balance \
     -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
   ```

   In Swagger UI, press **Authorize** and paste the `accessToken` (without the `Bearer ` prefix).

Tokens are HS256-signed JWTs that carry the customer id (`sub`), the display name (`name`) and expire after
`Jwt:AccessTokenLifetimeMinutes` (60 by default). There are no roles: registration is open so anyone can create their
own customer, and from then on every customer sees only their own records. This keeps the model simple and coherent
for a self-service API; an `Admin` role would only be needed for back-office operations that do not exist yet.

```bash
# Luis trying to read Ana's balance
curl -s http://localhost:5200/api/BankAccount/1000000001/balance -H "Authorization: Bearer <Luis' token>"
```

```json
{
  "title": "Access denied",
  "status": 403,
  "detail": "Account '1000000001' belongs to another customer.",
  "instance": "/api/BankAccount/1000000001/balance",
  "code": "AccountAccessDenied"
}
```

### Error responses

All errors use `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Insufficient funds",
  "status": 400,
  "detail": "Insufficient funds: the available balance is 20.00 and 20.01 was requested.",
  "instance": "/api/Transaction/Withdrawal",
  "code": "InsufficientFunds"
}
```

| Status | When                                                                                   | `code`                                                                                   |
| ------ | -------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `400`  | Invalid payload (`ValidationProblemDetails` with an `errors` map)                      | `ValidationFailed`                                                                       |
| `400`  | Business rule violated                                                                 | `InsufficientFunds`, `DepositBelowMinimum`, `AmountNotPositive`, `AmountTooManyDecimals`, `InterestRoundsToZero`, `SameAccountTransfer`, `BirthDateInFuture`, `PasswordTooShort`, … (see `DomainErrorCodes`) |
| `401`  | No valid bearer token (missing, expired or tampered; `WWW-Authenticate: Bearer` is set), or wrong login credentials | `AuthenticationRequired`, `InvalidCredentials`                      |
| `403`  | The account or customer belongs to another customer                                    | `AccountAccessDenied`, `CustomerAccessDenied`                                            |
| `404`  | Customer or account does not exist                                                     | `CustomerNotFound`, `AccountNotFound`                                                    |
| `409`  | Duplicate document number, or the account changed concurrently (retry the request)    | `DuplicateDocumentNumber`, `ConcurrencyConflict`, `AccountNumberTaken`                   |
| `429`  | Rate limit exceeded (`Retry-After` header says when to retry)                           | `TooManyRequests`                                                                        |
| `500`  | Unexpected error (details are logged, never returned)                                  | `UnexpectedError`                                                                        |

`code` is stable across languages and releases; `title` and `detail` are human-readable and may change.

Every problem response also carries the request's `correlationId` (see [Correlation id and rate limiting](#correlation-id-and-rate-limiting)).

### Localization

Error messages (`title`, `detail` and the validation `errors`) are returned in the language of the `Accept-Language`
header. Supported cultures are `en-US` (default) and `es-NI`; plain `en`/`es` and other regional variants (`en-GB`,
`es-MX`…) map to them, and the chosen culture is echoed in the `Content-Language` response header. Successful
responses are plain JSON and do not change with the language.

```bash
curl -s -X POST http://localhost:5200/api/Transaction/Withdrawal \
  -H "Content-Type: application/json" -H "Accept-Language: en" \
  -d '{ "accountNumber": "1000000002", "amount": 100000 }'
```

```json
{
  "title": "Insufficient funds",
  "status": 400,
  "detail": "Insufficient funds: the available balance is 800.00 and 100000.00 was requested.",
  "code": "InsufficientFunds"
}
```

```bash
curl -s -X POST http://localhost:5200/api/Transaction/Withdrawal \
  -H "Content-Type: application/json" -H "Accept-Language: es" \
  -d '{ "accountNumber": "1000000002", "amount": 100000 }'
```

```json
{
  "title": "Fondos insuficientes",
  "status": 400,
  "detail": "Fondos insuficientes: el saldo disponible es 800.00 y se solicitó 100000.00.",
  "code": "InsufficientFunds"
}
```

```bash
curl -s http://localhost:5200/api/Customer/999999 -H "Accept-Language: es-NI"
```

```json
{
  "title": "Recurso no encontrado",
  "status": 404,
  "detail": "No se encontró el cliente '999999'.",
  "code": "CustomerNotFound"
}
```

Texts live in two pairs of `.resx` files: [`Banking.API/Resources/ErrorMessages.resx`](Banking.API/Resources/ErrorMessages.resx)
(+ `.es.resx`) for problem titles and details, keyed by error code, and
[`Banking.Application/Resources/ValidationMessages.resx`](Banking.Application/Resources/ValidationMessages.resx)
(+ `.es.resx`) for the DataAnnotations messages of the request DTOs. A test checks that every key the code uses exists
in both languages with the same placeholders, so adding a language means adding one `.resx` per pair and the culture
to `SupportedCultures`.

### Correlation id and rate limiting

Every response carries an `X-Correlation-Id` header: the one the client sent (1 to 64 letters, digits, `.`, `-` or `_`)
or a generated one. The same id wraps every log line of the request (`CorrelationId` scope, enabled through
`Logging:Console:IncludeScopes`) and is returned as `correlationId` in problem responses, so a client can quote an error
and the server logs can be searched for it:

```bash
curl -s -D - http://localhost:5200/api/BankAccount/0000000000 -H "X-Correlation-Id: support-ticket-1234" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

```text
HTTP/1.1 404 Not Found
X-Correlation-Id: support-ticket-1234
Content-Type: application/problem+json

{ "title": "Resource not found", "status": 404, "code": "AccountNotFound", "correlationId": "support-ticket-1234", ... }
```

Two fixed-window rate limits protect the API, both configurable under `RateLimiting`:

| Policy             | Applies to                                             | Partition              | Default           |
| ------------------ | ------------------------------------------------------ | ---------------------- | ----------------- |
| `Authentication`   | `POST /api/Auth/login`, `POST /api/Customer`           | client IP address      | 10 per 60 seconds |
| `MoneyOperations`  | `Deposit`, `Withdrawal`, `Apply-Interest`              | authenticated customer | 30 per 60 seconds |

A rejected request gets a `429` problem response (`code: "TooManyRequests"`, translated like every other error) and a
`Retry-After` header with the seconds to wait. Reads are not limited. Behind a reverse proxy, enable forwarded headers
so the address partition sees the real client.

### Sample data (Development only)

When `Database:SeedSampleData` is `true` (it is in `appsettings.Development.json`) and the database has no customers,
two customers with one account each are created so you can try the API immediately. Both log in with the password
**`Banking123!`**:

| Customer        | Document number (login) | Account number | Ledger                                              |
| --------------- | ----------------------- | -------------- | --------------------------------------------------- |
| Ana Martínez    | `001-120490-0001A`      | `1000000001`   | open 500, deposit 250, withdraw 100, interest 65    |
| Luis Hernández  | `001-031185-0002B`      | `1000000002`   | open 1000, withdraw 200                             |

Customers created before passwords existed (a database from an earlier version) have an empty hash and cannot log in;
delete `Banking.Infrastructure/Data/banking.db` to let the seeder recreate the sample data, or register new customers.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (`global.json` rolls forward to the latest 10.0.x SDK installed)
- Visual Studio, Visual Studio Code, Rider, or any .NET compatible editor
- Or just [Docker](https://docs.docker.com/get-docker/), to run the API without installing the SDK (see below)

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

4. Log in (`POST /api/Auth/login`) as `001-120490-0001A` / `Banking123!`, press **Authorize** and paste the
   `accessToken`. Every other endpoint now works for Ana's account `1000000001`; Luis' account answers `403`.

Outside `Development` the API needs its own JWT signing key (see below) and refuses to start without it:

```bash
Jwt__SigningKey="$(openssl rand -base64 48)" ASPNETCORE_ENVIRONMENT=Production dotnet run --project Banking.API
```

### Running with Docker

The [`Dockerfile`](Dockerfile) is multi-stage: the SDK image restores, builds and publishes the API, and the final
image is the ASP.NET Core 8 runtime running as the non-root `app` user on port `8080`. The database is kept in a
named volume, so data survives container restarts and rebuilds.

```bash
docker compose up --build
```

Then open `http://localhost:8080/swagger` (Swagger is enabled because the compose file runs the API in
`Development`) or check `http://localhost:8080/health`. The compose file passes three settings as environment
variables, which you can override:

| Variable                 | Default                   | Purpose                                                          |
| ------------------------ | ------------------------- | ---------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT` | `Development`             | `Production` disables Swagger and rejects the development JWT key |
| `SEED_SAMPLE_DATA`       | `true`                    | Mapped to `Database__SeedSampleData`; insert the two sample accounts |
| `JWT_SIGNING_KEY`        | the development-only key  | Mapped to `Jwt__SigningKey`; **required in `Production`** (at least 32 characters) |

```bash
ASPNETCORE_ENVIRONMENT=Production SEED_SAMPLE_DATA=false JWT_SIGNING_KEY="$(openssl rand -base64 48)" docker compose up --build -d
```

`ConnectionStrings__DefaultConnection` is set to `Data Source=/data/banking.db`, the mounted volume. To start from an
empty database again:

```bash
docker compose down -v
```

#### Running the published image

Every release is also published to GitHub Container Registry as
[`ghcr.io/josueomar320/bankingapp`](https://github.com/JosueOmar320/BankingApp/pkgs/container/bankingapp), tagged with
the version (`1.0.0`), `latest` and the commit (`sha-<commit>`). The image runs in `Production`, so it needs its own
JWT signing key and keeps the database in `/data`:

```bash
docker run --rm -p 8080:8080 \
  -e Jwt__SigningKey="$(openssl rand -base64 48)" \
  -v banking-data:/data \
  ghcr.io/josueomar320/bankingapp:1.0.0
```

Then check `http://localhost:8080/health`. Swagger and the sample data are off outside `Development`; to try the
API from the browser with the two sample accounts, add `-e ASPNETCORE_ENVIRONMENT=Development` (the development
signing key ships with the image and is accepted only in that environment) and open `http://localhost:8080/swagger`.

### Configuration

| Key                                  | Default                                              | Purpose                                      |
| ------------------------------------ | ---------------------------------------------------- | -------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | `Data Source=../Banking.Infrastructure/Data/banking.db` | SQLite file; relative paths resolve against the API content root. The file is git-ignored and created on first run |
| `Database:SeedSampleData`            | `false` (`true` in Development)                      | Insert the sample accounts when the DB is empty |
| `RateLimiting:Authentication:PermitLimit` / `WindowSeconds` | `10` / `60`                   | Login and registration attempts per IP address and window |
| `RateLimiting:MoneyOperations:PermitLimit` / `WindowSeconds` | `30` / `60`                  | Deposits, withdrawals and interest per customer and window |
| `Logging:Console:IncludeScopes`      | `true`                                               | Print the `CorrelationId` scope on every log line |
| `Jwt:SigningKey`                     | development-only value in `appsettings.Development.json`; **required elsewhere** (`Jwt__SigningKey` environment variable, user secrets…) | HS256 secret, at least 32 characters. Start-up fails if it is missing or too short, and the committed development key (marked `DEVELOPMENT-ONLY`) is rejected outside `Development` |
| `Jwt:Issuer` / `Jwt:Audience`        | `BankingApp` / `BankingApp.API`                      | `iss` / `aud` claims; tokens with other values are rejected |
| `Jwt:AccessTokenLifetimeMinutes`     | `60`                                                 | Token lifetime (1 minute to 24 hours)        |

Pending migrations are applied automatically on start-up. If a previous run was killed while migrating (for example stopped from the IDE), the lock row EF Core leaves in `__EFMigrationsLock` is removed automatically once it is older than two minutes, so the API never hangs on start-up with the browser showing `ERR_CONNECTION_REFUSED`. To add a migration after changing the model:

```bash
dotnet ef migrations add <Name> --project Banking.Infrastructure --startup-project Banking.API
```

---

## Tests

- **Project:** `Banking.Tests`
- **Frameworks:** NUnit, Moq, EF Core SQLite (in-memory), `Microsoft.AspNetCore.Mvc.Testing`

| Layer          | What is covered                                                                                          |
| -------------- | -------------------------------------------------------------------------------------------------------- |
| Domain         | Opening deposit rules, amount validation, insufficient funds, interest rounding, version bumps, password policy and hashing |
| Application    | Services with mocked repositories: mapping, not-found, **ownership checks (403)**, login (wrong password, unknown document), retry of account-number generation, summaries |
| Infrastructure | Real SQLite: atomic save of balance + ledger, unique document/account numbers, **concurrency conflict**, migrations in sync with the model, PBKDF2 hasher, seeded customers can log in |
| API            | In-process HTTP tests: login, **401 without a token, 403 with another customer's token, 200 with the owner's**, status codes, `Location` headers, problem details, validation, **parallel withdrawals never overdraw**, the same error in English and Spanish, `.resx` keys in sync, Swagger bearer scheme, start-up fails without a signing key |

```bash
dotnet test
```

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every push and pull request: `dotnet format` check,
build with `TreatWarningsAsErrors`, the full test suite and a NuGet vulnerability audit.
[Dependabot](.github/dependabot.yml) opens a weekly pull request for NuGet packages and GitHub Actions, grouping minor
and patch updates together.

## Releases

Versions follow [Semantic Versioning](https://semver.org) and every change is recorded in the
[CHANGELOG](CHANGELOG.md) ([Keep a Changelog](https://keepachangelog.com) format). The version lives in one place,
`<Version>` in [`Directory.Build.props`](Directory.Build.props), so the assemblies carry it and Swagger shows it in
the document title.

To publish a release: move the `[Unreleased]` entries of the changelog under a new `[x.y.z]` heading dated today,
set the same version in `Directory.Build.props`, merge, and push the tag:

```bash
git tag vX.Y.Z && git push origin vX.Y.Z
```

[`.github/workflows/release.yml`](.github/workflows/release.yml) then checks that the tag matches the version,
builds the Docker image, pushes it to `ghcr.io/josueomar320/bankingapp` (`X.Y.Z`, `latest`, `sha-<commit>`) and
creates the [GitHub Release](https://github.com/JosueOmar320/BankingApp/releases) with that version's changelog
section as its notes.

## License

[MIT](LICENSE) © 2026 Josue Omar Flores
