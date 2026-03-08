# 🏦 BankAPI — ASP.NET Core Banking REST API

[![.NET CI](https://github.com/soyRex-codes/web_app_Csharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/soyRex-codes/web_app_Csharp/actions/workflows/dotnet.yml)

![C#](https://img.shields.io/badge/C%23-10-blue?logo=csharp)
![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-10-green?logo=nuget)
![Docker](https://img.shields.io/badge/Docker-Ready-blue?logo=docker)
![License](https://img.shields.io/badge/License-MIT-yellow)

A full-stack banking REST API built with **ASP.NET Core**, **Entity Framework Core**, and **SQLite**. Demonstrates OOP principles (Encapsulation, Inheritance, Polymorphism), LINQ queries, Dependency Injection, and structured logging.

---

## 🏗️ Architecture

```
web_app_Csharp/
├── Controllers/
│   └── BankControllers.cs      # REST API endpoints (CRUD operations)
├── Models/
│   └── Account.cs              # Domain models (SecureAccount → CheckingAccount, SavingAccount, RetirementAccount)
├── Data/
│   └── BankContext.cs           # EF Core DbContext (Table-Per-Hierarchy inheritance)
├── Migrations/                  # EF Core database migrations
├── Program.cs                   # Application entry point & DI configuration
├── Dockerfile                   # Container configuration
└── appsettings.json             # App configuration
```

### Design Patterns Used
- **Repository Pattern** via EF Core `DbContext`
- **Dependency Injection** for `BankContext` and `ILogger<T>`
- **Table-Per-Hierarchy (TPH)** inheritance mapping — one table for all account types with a `Discriminator` column
- **Template Method** via `virtual` Deposit/Withdraw overrides in `RetirementAccount`

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/Bank/debtors` | Get email addresses of accounts with negative balance |
| `GET` | `/api/Bank/CheckingAccount` | List all account owners |
| `POST` | `/api/Bank/CheckingAccount` | Create a new checking account |
| `POST` | `/api/Bank/deposit?owner=Name&amount=100` | Deposit funds into an account |
| `POST` | `/api/Bank/withdraw_funds?owner=Name&amount=50` | Withdraw funds from an account |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)

### Build & Run
```bash
# Clone the repository
git clone https://github.com/soyRex-codes/web_app_Csharp.git
cd web_app_Csharp

# Restore dependencies and build
dotnet build

# Run the application (opens Swagger UI automatically)
dotnet run --project web_app_Csharp
```

### Run Tests
```bash
dotnet test --verbosity normal
```

### Docker
```bash
docker compose up --build
```

---

## 🧪 Testing

Unit tests are written with **xUnit** following the **Arrange-Act-Assert** pattern:

- ✅ `Deposit_ValidAmount_IncreasesBalance`
- ✅ `Deposit_NegativeAmount_ThrowsInvalidOperationException`
- ✅ `Withdraw_MoreThanBalance_ThrowsInvalidOperationException`
- ✅ `Withdraw_ValidAmount_DecreasesBalance`

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Language** | C# 10 |
| **Framework** | ASP.NET Core (.NET 10) |
| **ORM** | Entity Framework Core 10 |
| **Database** | SQLite (swappable to SQL Server) |
| **API Docs** | Swagger / OpenAPI |
| **Testing** | xUnit |
| **CI/CD** | GitHub Actions |
| **Containerization** | Docker |

---

## 📝 Future Enhancements

- [ ] SQL Server integration for production
- [ ] React + TypeScript frontend
- [ ] JWT authentication & authorization
- [ ] Transfer endpoint between accounts
- [ ] Account balance history / transaction log
