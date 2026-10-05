# BankingApp: .NET 8 Banking API

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-SQLite-003B57?logo=sqlite&logoColor=white)
![Tests](https://img.shields.io/badge/tests-NUnit_%2B_Moq-25A162)
![Swagger](https://img.shields.io/badge/docs-Swagger-85EA2D?logo=swagger&logoColor=black)

This project is a **Banking API** developed in **.NET 8** using **ASP.NET Core Web API**. It allows for the management of clients, bank accounts, and transactions (deposits, withdrawals, and interest calculation).

The database is already created and contains **test accounts**, so you only need to run the API.

---

## Features

- Create client profile
- Create a bank account associated with a client
- Check account balance
- Register deposits and withdrawals
- Apply interest on the balance
- View transaction history and final balance summary
- Integrity validations: initial deposit of at least 10, withdrawal cannot exceed available balance
- Unit tests with **NUnit** and **Moq**
- Integrated Swagger documentation

### Accounts

- The **interest rate is fixed at 10%** for all accounts.

---

## Architecture

The solution follows **Clean Architecture**, with dependencies pointing inward:

| Project                  | Responsibility                                                  |
| ------------------------ | --------------------------------------------------------------- |
| `Banking.Domain`         | Entities, enums and repository interfaces                       |
| `Banking.Application`    | Services with the business rules, DTOs and service interfaces   |
| `Banking.Infrastructure` | EF Core `DbContext`, SQLite database, migrations, repositories  |
| `Banking.API`            | Controllers, dependency injection and Swagger                   |
| `Banking.Tests`          | Unit tests for controllers, services and repositories           |

## Endpoints

| Method | Route                                     | Description                         |
| ------ | ----------------------------------------- | ----------------------------------- |
| `POST` | `/api/Customer`                           | Create a client                     |
| `POST` | `/api/BankAccount`                        | Create a bank account for a client  |
| `GET`  | `/api/BankAccount/{accountNumber}/balance` | Check an account's balance          |
| `POST` | `/api/Transaction/Deposit`                | Register a deposit                  |
| `POST` | `/api/Transaction/Withdrawal`             | Register a withdrawal               |
| `POST` | `/api/Transaction/Apply-Interest`         | Apply interest to the balance       |
| `GET`  | `/api/Transaction/{accountNumber}/summary` | Transaction history and final balance |

The full request and response schemas are available in Swagger.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed
- Visual Studio, Visual Studio Code, Rider, or any .NET compatible editor

## Project Execution

1. Clone the repository:

   ```bash
   git clone https://github.com/JosueOmar320/BankingApp.git
   cd BankingApp
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Run the API:

   ```bash
   dotnet run --project Banking.API
   ```

   The API will run at:

   ```text
   https://localhost:7201
   http://localhost:5200
   ```

4. Open Swagger to test the endpoints in your browser:

   ```text
   https://localhost:7201/swagger
   http://localhost:5200/swagger
   ```

## Unit Tests

- **Test Project:** `Banking.Tests`
- **Frameworks used:** NUnit, Moq, EF Core InMemory (repository tests)

## Test Coverage

The unit tests include the following scenarios:

- **Client and account creation**
- **Deposit and withdrawal operations**
- **Interest application** (fixed 10% rate)
- **Balance inquiries and transaction summaries**
- **Error validation:**
  - Insufficient funds
  - Negative or zero amounts
  - Mandatory data validations
  - Account creation for a client that doesn't exist

### Run all tests

```bash
dotnet test
```

### Run tests with details

```bash
dotnet test --logger "console;verbosity=detailed"
```
