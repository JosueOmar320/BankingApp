# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-18

First release of the Banking API: a .NET 8 / ASP.NET Core Web API with Clean Architecture, a rich domain model,
EF Core 9 on SQLite, JWT authentication, localized errors, rate limiting and transfers between accounts. The
sections below describe the changes against the original, unreleased code base.

### Added

- **Rich domain model.** `BankAccount` is the aggregate that owns the ledger: `Open`, `Deposit`, `Withdraw`,
  `ApplyInterest` and `TransferTo` enforce the business rules (`AccountRules`), record the `Transaction` and bump a
  concurrency `Version`. `Customer.Create` validates names, document, birth date, income and the password policy.
  The Domain project has no framework dependencies ([#1](https://github.com/JosueOmar320/BankingApp/pull/1),
  [#5](https://github.com/JosueOmar320/BankingApp/pull/5), [#18](https://github.com/JosueOmar320/BankingApp/pull/18)).
- **Endpoints** `GET /api/Customer/{id}`, `GET /api/BankAccount/{accountNumber}` and `GET /health`
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- **JWT authentication and per-owner authorization.** `POST /api/Auth/login` returns an HS256 bearer token
  (`sub` = customer id, `name`, 60 minutes by default). A fallback policy requires an authenticated customer on every
  endpoint unless it opts out with `[AllowAnonymous]` (registration, login, health). Each use case loads the entity
  and compares its owner with the caller, so a missing record is `404` and someone else's is `403`
  (`AccountAccessDenied`, `CustomerAccessDenied`) ([#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- **Transfers between accounts.** `POST /api/Transaction/Transfer` debits the caller's source account
  (`TransferOut`) and credits any existing destination (`TransferIn`) in one database transaction, bumping both
  concurrency versions. The summary gains `totalTransfersIn` and `totalTransfersOut`
  ([#18](https://github.com/JosueOmar320/BankingApp/pull/18)).
- **Paginated transaction summary.** `GET /api/Transaction/{accountNumber}/summary` accepts `page` (default 1) and
  `pageSize` (default 50, maximum 200), returns one page of transactions oldest first plus a `page` object
  (`number`, `size`, `totalItems`, `totalPages`); the totals are computed by the database over the whole ledger
  ([#16](https://github.com/JosueOmar320/BankingApp/pull/16)).
- **Localized error messages.** `title`, `detail` and validation `errors` are returned in English (`en-US`, default)
  or Spanish (`es-NI`) according to `Accept-Language`; plain `en`/`es` and other regional variants map to them and the
  chosen culture is echoed in `Content-Language`. Every problem response carries a stable `code` extension
  (`InsufficientFunds`, `CustomerNotFound`, `ValidationFailed`, …) so clients never parse text
  ([#3](https://github.com/JosueOmar320/BankingApp/pull/3)).
- **Correlation id.** Every response carries an `X-Correlation-Id` header (the client's when well-formed, otherwise a
  generated one); it becomes the `TraceIdentifier`, wraps the request's log lines in a `CorrelationId` scope and is
  returned as `correlationId` in every problem response ([#17](https://github.com/JosueOmar320/BankingApp/pull/17)).
- **Swagger UI** with XML documentation, an Authorize button for the bearer token, a padlock and documented
  `401`/`403` on protected operations, and the `Accept-Language` header on every operation
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1), [#3](https://github.com/JosueOmar320/BankingApp/pull/3),
  [#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- **Automatic database set-up.** Pending migrations are applied on start-up (`DatabaseInitializer`) and, when
  `Database:SeedSampleData` is `true` (Development), two sample customers with one account each are inserted
  (password `Banking123!`) ([#1](https://github.com/JosueOmar320/BankingApp/pull/1),
  [#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- **Docker.** Multi-stage `Dockerfile` (SDK 8.0 build, ASP.NET Core 8.0 runtime, non-root `app` user, port 8080,
  `curl` health check on `/health`) and `docker-compose.yml` with a named volume for the SQLite file and
  `ASPNETCORE_ENVIRONMENT`, `SEED_SAMPLE_DATA` and `JWT_SIGNING_KEY` as overridable variables
  ([#4](https://github.com/JosueOmar320/BankingApp/pull/4), [#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- **Tooling.** `Directory.Build.props` (nullable, analyzers at `latest-recommended`, warnings as errors, code style
  enforced in build, deterministic builds), `.editorconfig`, `global.json`, `.dockerignore`, MIT `LICENSE`
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1), [#4](https://github.com/JosueOmar320/BankingApp/pull/4)).
- **Continuous integration.** GitHub Actions workflow (`build-and-test`): `dotnet format` check, Release build with
  warnings as errors, the full test suite and a NuGet vulnerability audit on every push and pull request
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- **Dependabot** for NuGet packages and GitHub Actions, weekly, with minor and patch updates grouped
  ([#4](https://github.com/JosueOmar320/BankingApp/pull/4)).
- **Tests.** 274 tests (up from 39): domain rules, services with mocked repositories, repositories on real SQLite
  (atomic saves, unique indexes, concurrency conflicts, migrations in sync with the model) and in-process HTTP tests
  (status codes, problem details, `401`/`403`, localization, rate limits, parallel withdrawals and transfers that never
  overdraw) ([#1](https://github.com/JosueOmar320/BankingApp/pull/1) to
  [#18](https://github.com/JosueOmar320/BankingApp/pull/18)).

### Changed

- **Money operations are atomic.** The balance change and its ledger entry are saved in a single `SaveChanges`, and
  the opening deposit is recorded in the ledger (`startingBalance` is 0 for new accounts)
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- **Breaking:** `Deposit`, `Withdrawal` and `Apply-Interest` take a JSON body instead of query string parameters
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- **Breaking:** every error is an RFC 7807 `application/problem+json` response produced by a single
  `IExceptionHandler` (`400` business rule, `401`, `403`, `404`, `409`, `429`, `500` without leaking internals);
  `ErrorResponseDto` and the anonymous `{ message }` payload are gone, and the automatic validation `400` is also
  `application/problem+json` ([#1](https://github.com/JosueOmar320/BankingApp/pull/1),
  [#3](https://github.com/JosueOmar320/BankingApp/pull/3)).
- **Breaking:** `GET /api/BankAccount/{accountNumber}/balance` returns `{ accountNumber, balance }` instead of a raw
  number; `BankAccountResponseDto` and `TransactionResponseDto` carry more fields
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- **Breaking:** `POST /api/Customer` requires a `password` of 8 to 128 characters, and `POST /api/BankAccount` only
  opens accounts for the caller's own `customerId` (`403` otherwise)
  ([#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- **New business rules:** amounts accept at most two decimals, interest is rounded to cents and a result of zero is
  rejected, a transfer needs two different accounts, and the document number is unique per customer (`409`)
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1), [#18](https://github.com/JosueOmar320/BankingApp/pull/18)).
- **Persistence.** Entities carry no data annotations; mapping, unique indexes on account and document numbers,
  restrictive foreign keys, the ledger index, the UTC date converter and the concurrency token live in
  `IEntityTypeConfiguration` classes. Migrations `AddConcurrencyAndUniqueConstraints` and `AddCustomerPasswordHash`
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1), [#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- **The SQLite database file is no longer tracked** in the repository (`*.db` is ignored); it is created on first run.
  The connection string uses forward slashes and relative paths resolve against the API content root
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1), [#4](https://github.com/JosueOmar320/BankingApp/pull/4)).
- **Dependencies.** EF Core 9.0.21, Swashbuckle.AspNetCore 6.9.0, Moq 4.21.0, coverlet.collector 10.1.0,
  `actions/checkout` v7, `actions/setup-dotnet` v6, `actions/upload-artifact` v7; EF Core InMemory removed from the
  tests. Dependabot ignores major updates of `Microsoft.EntityFrameworkCore*`, `Microsoft.Extensions.*` and
  `Microsoft.AspNetCore.*`, which belong to a deliberate .NET 10 upgrade rather than a weekly bump
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1), [#6](https://github.com/JosueOmar320/BankingApp/pull/6),
  [#7](https://github.com/JosueOmar320/BankingApp/pull/7), [#8](https://github.com/JosueOmar320/BankingApp/pull/8),
  [#9](https://github.com/JosueOmar320/BankingApp/pull/9), [#10](https://github.com/JosueOmar320/BankingApp/pull/10),
  [#15](https://github.com/JosueOmar320/BankingApp/pull/15)).
- README rewritten in English: architecture, design decisions, endpoints, authentication, error contract,
  localization, Docker, configuration and tests.

### Fixed

- Two concurrent withdrawals could both pass validation and overdraw an account; accounts now carry an optimistic
  concurrency token checked on every update, so the loser gets `409 Conflict` and can retry
  ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- A ledger entry and its balance change could be persisted independently, leaving the history out of sync with the
  balance ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- Account and document numbers were not unique in the database ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- The API hung on start-up (`ERR_CONNECTION_REFUSED`) when a previous run was killed while migrating: stale rows in
  `__EFMigrationsLock` older than two minutes are now removed before migrating
  ([#2](https://github.com/JosueOmar320/BankingApp/pull/2)).
- Inconsistent error payloads, a missing `Location` header on `201` responses, the `CancellationToken` ignored in
  account reads, dates returned without the UTC `Z` suffix, interest computed with unbounded decimals, and a Windows-only
  relative connection string ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).

### Security

- Customers authenticate with a JWT and can only operate their own accounts and records
  ([#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- Passwords are stored only as PBKDF2 hashes (ASP.NET Core Identity's hasher); login never reveals which credential
  was wrong and an unknown document costs a hash too, to avoid timing-based enumeration
  ([#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- `Jwt:SigningKey` is validated at start-up (required, at least 32 characters) and the committed `DEVELOPMENT-ONLY`
  key is rejected outside the Development environment ([#5](https://github.com/JosueOmar320/BankingApp/pull/5)).
- Fixed-window rate limits on login and registration (per client IP) and on money operations (per customer), with a
  translated `429` problem and `Retry-After` ([#17](https://github.com/JosueOmar320/BankingApp/pull/17)).
- The transitive vulnerability GHSA-2m69-gcr7-jv3q (`SQLitePCLRaw.lib.e_sqlite3` 2.1.10) is resolved by moving to EF
  Core 9.0.21, and CI fails on any known vulnerable package ([#1](https://github.com/JosueOmar320/BankingApp/pull/1)).
- The Docker image runs as the non-root `app` user ([#4](https://github.com/JosueOmar320/BankingApp/pull/4)).

[Unreleased]: https://github.com/JosueOmar320/BankingApp/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/JosueOmar320/BankingApp/releases/tag/v1.0.0
