# BankingApp

[![.NET CI](https://github.com/soyRex-codes/web_app_Csharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/soyRex-codes/web_app_Csharp/actions/workflows/dotnet.yml)

A small banking API and Razor Pages demo built with C# 14, .NET 10, ASP.NET Core Minimal APIs, ASP.NET Core Identity, Entity Framework Core, and SQL Server. It demonstrates account ownership, money-operation rules, immutable transaction history, and atomic transfers without unnecessary layers.

## What it does

- Register, log in, and log out with ASP.NET Core Identity cookie authentication; passwords are hashed by Identity and never stored or logged by application code.
- Assign every new user the `Customer` role; support an `Admin` role for administrative access.
- Create checking or savings accounts owned by the authenticated user.
- Deposit, withdraw, transfer, and view transaction history for authorized accounts.
- Provide Razor Pages for registration, sign-in, account workflows, transaction history, and the admin account list.
- Return RFC 7807 Problem Details for validation, authentication, authorization, missing-resource, and insufficient-funds failures.
- Run tests, formatting verification, dependency auditing, container builds, and measured coverage in GitHub Actions.

This is a portfolio project, not a production banking system. See [deliberate scope](#deliberate-scope) for the intentional limits.

## Architecture

```text
Banking request
    ↓
Minimal API endpoint or Razor Page handler
    ↓
AccountOperationsService
    ↓
BankAccount domain behavior and authorization checks
    ↓
EF Core BankContext
    ↓
SQL Server
```

The application is a modular monolith organized by feature. EF Core's `DbContext` is used directly as the unit of work. A single focused `AccountOperationsService` shares banking workflows between Minimal APIs and Razor Pages; there is no generic repository or generic service layer. Read the [architecture note](docs/architecture.md) for the important design decisions.

```text
web_app_Csharp/
├── Data/
│   ├── Configurations/
│   ├── Migrations/
│   └── BankContext.cs
├── Features/
│   ├── Accounts/
│   ├── Identity/
│   └── Transfers/
├── Pages/
├── wwwroot/
├── Program.cs
└── Dockerfile
```

## Prerequisites

- .NET SDK 10
- Docker Desktop and Docker Compose for the containerized setup
- SQL Server, either through Docker Compose or a local/remote instance

The repository includes the local `dotnet-ef` tool. Restore it once after cloning:

```bash
dotnet tool restore
```

## Run with Docker Compose

Copy the local-only environment template, then build and start the API and SQL Server:

```bash
cp -n .env.example .env
docker compose up --build
```

The API listens at `http://localhost:8080`. In Development, its OpenAPI document is available at:

```text
http://localhost:8080/openapi/v1.json
```

Verify it with:

```bash
curl -i http://localhost:8080/openapi/v1.json
```

## Browser demo

The Razor Pages demo is served by the same application and uses the Identity cookie. Its banking forms use the same focused account-operations service as the APIs, so the browser and API enforce the same rules without a second frontend stack.

- [Register](http://localhost:8080/register) a Customer user.
- [Sign in](http://localhost:8080/login), then open [My Accounts](http://localhost:8080/accounts) to create and manage checking or savings accounts.
- Deposit, withdraw, transfer to another owned account, and review transaction history from an account's **Manage** page.
- A Development-seeded admin can open [All Accounts](http://localhost:8080/admin/accounts).

Stop the services while preserving the local SQL Server volume:

```bash
docker compose down
```

`docker compose down -v` deletes the local database volume and should be used only when a reset is intended. On Apple Silicon, SQL Server runs under `linux/amd64` emulation, so its initial startup can take longer.

## Run from the .NET CLI

Start SQL Server only:

```bash
cp -n .env.example .env
docker compose up -d sqlserver
```

Set the development connection string with user secrets. Do not put passwords in `appsettings.json` or commit a local `.env` file.

```bash
dotnet user-secrets set \
  --project web_app_Csharp \
  "ConnectionStrings:BankDatabase" \
  "Server=localhost,1433;Database=BankingApp;User Id=sa;Password=ChangeThisLocalPassword!123;Encrypt=True;TrustServerCertificate=True"
```

Run the API in Development:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project web_app_Csharp
```

Development startup applies pending migrations automatically. Production deployments should apply migrations as a separate deployment step.

## Deploy the portfolio demo

The live demo uses Azure Container Apps for the application and Azure SQL Database for persistent data. Docker Compose remains a local-development setup; it does not deploy the SQL Server container to the public internet.

Before the first release, create an Azure SQL database and apply migrations from a trusted machine. Copy its ADO.NET connection string from the Azure portal, replace its password placeholder locally, and store it only in user secrets:

```bash
dotnet user-secrets set \
  --project web_app_Csharp \
  "ConnectionStrings:BankDatabase" \
  "<Azure SQL ADO.NET connection string>"

dotnet ef database update --project web_app_Csharp --startup-project web_app_Csharp
```

In non-development environments, ASP.NET Core Data Protection keys are stored in the same SQL database. This preserves Identity cookie sessions when a container revision restarts or scales to zero.

The **Deploy portfolio demo** workflow publishes a Linux `amd64` image to GitHub Container Registry and, after configuration, updates the Azure Container App only after `.NET CI` succeeds on `main`.

For the first image, run the workflow manually with **deploy** unchecked. Make the resulting `bankingapp` package public in its GitHub package settings, then create an Azure Container App from that image with:

- external ingress on target port `8080`;
- `ASPNETCORE_ENVIRONMENT=Production` and `ASPNETCORE_HTTP_PORTS=8080` environment variables;
- a `ConnectionStrings__BankDatabase` secret containing the Azure SQL connection string, referenced by the container environment variable of the same name;
- minimum replicas `0` and maximum replicas `1` for the low-traffic demo.

After the Container App exists, configure these repository-level GitHub Actions variables: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP`, and `AZURE_CONTAINER_APP_NAME`. Use Azure workload identity federation for the GitHub Actions identity; grant it `Contributor` only on this demo resource group. No Azure password, SQL connection string, or client secret belongs in the repository or GitHub Actions variables.

Run the workflow once more with **deploy** checked to deploy the current image. Later successful CI runs on `main` publish and deploy automatically. The first request after an idle period can be slower because Container Apps is allowed to scale to zero.

## Development admin

New registrations receive only the `Customer` role. For local development, an admin can be seeded only when both values are stored in user secrets:

```bash
dotnet user-secrets set --project web_app_Csharp "Identity:BootstrapAdmin:Email" "admin@example.test"
dotnet user-secrets set --project web_app_Csharp "Identity:BootstrapAdmin:Password" "Portfolio1!"
```

The bootstrap logic runs only in the Development environment. Restart the API after setting these values. There is intentionally no public endpoint for assigning the `Admin` role.

## Migrations

List and apply the checked-in migrations:

```bash
dotnet ef migrations list --project web_app_Csharp --startup-project web_app_Csharp
dotnet ef database update --project web_app_Csharp --startup-project web_app_Csharp
```

When a deliberate model change requires a migration:

```bash
dotnet ef migrations add MigrationName --project web_app_Csharp --startup-project web_app_Csharp --output-dir Data/Migrations
```

Review the generated migration before committing it.

## API

The OpenAPI document is the complete endpoint reference in Development. The main routes are:

| Method | Endpoint | Authentication | Description |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Anonymous | Create a Customer user |
| `POST` | `/api/v1/auth/login` | Anonymous | Start an Identity cookie session |
| `POST` | `/api/v1/auth/logout` | Signed in | End the current session |
| `GET` | `/api/v1/accounts` | Signed in | List owned accounts; admins list all |
| `GET` | `/api/v1/accounts/{id}` | Signed in | Read an owned account; admins may read all |
| `POST` | `/api/v1/accounts` | Signed in | Create an account for the current user |
| `POST` | `/api/v1/accounts/{id}/deposits` | Authorized account | Deposit funds |
| `POST` | `/api/v1/accounts/{id}/withdrawals` | Authorized account | Withdraw funds |
| `GET` | `/api/v1/accounts/{id}/transactions` | Authorized account | Read history, newest first |
| `POST` | `/api/v1/transfers` | Authorized accounts | Transfer between two owned accounts; admins may use any accounts |

Account and transfer operations require the cookie returned by login. The account owner is taken from the signed-in user; clients cannot choose an `ownerId` when creating an account.

### Example workflow with curl

Register a customer:

```bash
curl -i -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"customer@example.test","password":"Portfolio1!"}'
```

Log in and save the cookie locally:

```bash
curl -i -c cookies.txt -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"customer@example.test","password":"Portfolio1!"}'
```

Create an account with that cookie:

```bash
curl -i -b cookies.txt -X POST http://localhost:8080/api/v1/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"Everyday Checking","type":"Checking"}'
```

Deposit money, replacing `1` with the created account ID:

```bash
curl -i -b cookies.txt -X POST http://localhost:8080/api/v1/accounts/1/deposits \
  -H "Content-Type: application/json" \
  -d '{"amount":500.00}'
```

Transfer money between two accounts owned by the same customer:

```bash
curl -i -b cookies.txt -X POST http://localhost:8080/api/v1/transfers \
  -H "Content-Type: application/json" \
  -d '{"fromAccountId":1,"toAccountId":2,"amount":125.00}'
```

See [`web_app_Csharp/web_app_Csharp.http`](web_app_Csharp/web_app_Csharp.http) for an IDE-friendly version of these requests.

### Error contract

Errors use `application/problem+json` and include a `traceId`.

| Status | Meaning |
|---|---|
| `400` | Invalid request or monetary amount |
| `401` | No valid authenticated session |
| `403` | Signed-in customer tried to access another customer's account |
| `404` | Account does not exist |
| `409` | Insufficient funds |

## Test and CI commands

Run the same core checks used by the pull-request workflow:

```bash
dotnet restore web_app_Csharp.sln
dotnet build web_app_Csharp.sln --configuration Release --no-restore
dotnet format web_app_Csharp.sln --verify-no-changes --no-restore
dotnet test web_app_Csharp.sln --configuration Release --no-build
```

GitHub Actions also audits NuGet dependencies, builds the container image, and uploads Cobertura coverage as an artifact. Coverage is measured for visibility only; it is not a quality gate.

## Deliberate scope

- Monetary values use `decimal(18,2)` and reject fractional cents. The demo displays amounts as USD; multi-currency and conversion are out of scope.
- Account transactions record successful deposits, withdrawals, and transfers. They are not a complete external audit or reconciliation system.
- Authentication uses same-application cookies for the Razor Pages shell. There are no JWT refresh tokens, external identity providers, email confirmation, or password-reset flows.
- Account ownership is enforced in the API. Admin provisioning beyond the Development bootstrap is intentionally outside this project.
- Transfers use one database transaction, but the project does not yet implement idempotency keys or cross-system payment processing.
- Automatic migrations run only in Development. Production database migrations are a deliberate release step; monitoring infrastructure and external payment integrations are out of scope.
