# BankingApp

[![.NET CI](https://github.com/soyRex-codes/web_app_Csharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/soyRex-codes/web_app_Csharp/actions/workflows/dotnet.yml)

A portfolio banking API built with C# 14, .NET 10, ASP.NET Core Minimal APIs, Entity Framework Core, and SQL Server.

The current implementation focuses on a small, defensible account domain: account creation, deposits, withdrawals, persistence, consistent HTTP responses, and automated tests. Authentication, transfers, and transaction history will be added as separate reviewable features.

## Current capabilities

- Async Minimal API endpoints grouped under `/api/v1/accounts`
- Encapsulated account balance with validated deposit and withdrawal operations
- Checking and savings account types
- SQL Server persistence through EF Core migrations
- DTO-based API contracts that do not expose EF entities directly
- RFC Problem Details responses for validation and business-rule failures
- Structured logging through `ILogger`
- OpenAPI document generation in development
- xUnit domain tests
- Docker Compose environment for the API and SQL Server
- GitHub Actions build, test, dependency-audit, and container-build checks

## Architecture

```text
HTTP request
    ↓
Minimal API endpoint + request/response DTOs
    ↓
BankAccount domain behavior
    ↓
EF Core BankContext
    ↓
SQL Server
```

The application is a modular monolith organized by feature. EF Core's `DbContext` is used directly as the unit of work; the project does not add a generic repository wrapper.

```text
web_app_Csharp/
├── Data/
│   ├── Configurations/
│   ├── Migrations/
│   └── BankContext.cs
├── Features/
│   └── Accounts/
│       ├── AccountContracts.cs
│       ├── AccountEndpoints.cs
│       ├── AccountType.cs
│       └── BankAccount.cs
├── Program.cs
└── Dockerfile
```

## API

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/accounts` | List accounts |
| `GET` | `/api/v1/accounts/{id}` | Retrieve one account |
| `POST` | `/api/v1/accounts` | Create an account |
| `POST` | `/api/v1/accounts/{id}/deposits` | Deposit funds |
| `POST` | `/api/v1/accounts/{id}/withdrawals` | Withdraw funds |

Example account request:

```json
{
  "ownerId": "user-123",
  "name": "Everyday Checking",
  "type": "Checking"
}
```

The API currently uses an external-looking `ownerId` in preparation for ASP.NET Core Identity. Authorization is not implemented yet, so this version must not be treated as a production banking system.

## Run with Docker Compose

Prerequisites:

- Docker Desktop
- Docker Compose

Create the local environment file and start both services:

```bash
cp .env.example .env
docker compose up --build
```

The API is available at `http://localhost:8080`. Its development OpenAPI document is available at `http://localhost:8080/openapi/v1.json`.

SQL Server runs through x64 emulation on Apple Silicon because Microsoft's SQL Server Linux image targets `linux/amd64`.

## Run from the .NET CLI

Start only SQL Server:

```bash
cp .env.example .env
docker compose up -d sqlserver
```

Configure the local connection string without committing credentials:

```bash
dotnet user-secrets set \
  --project web_app_Csharp \
  "ConnectionStrings:BankDatabase" \
  "Server=localhost,1433;Database=BankingApp;User Id=sa;Password=ChangeThisLocalPassword!123;Encrypt=True;TrustServerCertificate=True"
```

Restore tools and run the API:

```bash
dotnet tool restore
dotnet run --project web_app_Csharp
```

Development startup applies pending EF Core migrations automatically. Production deployments should apply migrations as a separate deployment step.

## Test and inspect dependencies

```bash
dotnet test web_app_Csharp.sln
dotnet package list \
  --project web_app_Csharp/web_app_Csharp.csproj \
  --vulnerable \
  --include-transitive
```

## Deliberate scope

- Monetary values use SQL Server `decimal(18,2)` and the domain rejects fractional cents.
- The application supports USD-denominated balances only for now.
- Account type is stored as a readable string in SQL Server.
- `OwnerId` is indexed and sized for a future ASP.NET Core Identity relationship.
- Automatic migrations are limited to the development environment.

## Planned features

- ASP.NET Core Identity and policy-based account ownership
- Transfers with atomic EF Core transactions
- Immutable transaction history
- API integration tests and measured coverage reporting
- Production deployment after the application security model is complete
