# BankingApp
# .NET 8 Banking API

This project is a **Banking API** developed in **.NET 8** using **ASP.NET Web API**. It allows for the management of clients, bank accounts, and transactions (deposits, withdrawals, and interest calculation).

The database is already created and contains **test accounts**, so you only need to run the API.

---

## Features

- Create client profile
- Create a bank account associated with a client
- Check account balance
- Register deposits and withdrawals
- Apply interest on the balance
- View transaction history and final balance summary
- Integrity validations: initial balance greater than 0, withdrawal cannot exceed available balance
- Unit tests with **NUnit** and **Moq**
- Integrated Swagger documentation

---

### Accounts

- The **interest rate is fixed at 10%** for all accounts.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed
- Visual Studio, Visual Studio Code, or any .NET compatible editor

## Project Execution
1. Clone the repository:
   ```bash
   git clone [https://github.com/user/BankingApp.git](https://github.com/user/BankingApp.git)
   cd BankingApp
2. Restore dependencies:
  ```bash
  dotnet restore
  ```

3. Run the API:
  ```bash
  dotnet run --project Banking.Api
  ```

The API will run at:
  ```bash
  https://localhost:7201
  http://localhost:5200
  ```
  
4. Open Swagger to test the endpoints in your browser:
  ```bash
   https://localhost:7201/swagger
   http://localhost:5200/swagger
  ```

## Unit Tests

- **Test Project:** `Banking.Tests`
- **Frameworks used:** NUnit, Moq

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

### Run all tests
```bash
dotnet test
```

### Run tests with details
```bash
dotnet test --logger "console;verbosity=detailed"
```






