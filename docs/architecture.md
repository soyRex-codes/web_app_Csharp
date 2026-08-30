# Architecture notes

## Request flow

The application is a modular monolith. Minimal API endpoints and Razor Pages live beside their feature contracts. Banking workflows use a focused service with `BankContext` and execute against SQL Server through Entity Framework Core.

```text
Minimal API endpoint or Razor Page handler
    ↓
AccountOperationsService
    ↓
Request contract, authorization check, and BankAccount domain operation
    ↓
BankContext / EF Core
    ↓
SQL Server
```

`DbContext` already tracks changes and coordinates `SaveChangesAsync`, so the project does not wrap it in a generic repository or unit-of-work abstraction. `AccountOperationsService` is intentionally focused on the banking workflows shared by Minimal APIs and Razor Pages, rather than a generic service layer.

## Encapsulated balances

`BankAccount.Balance` has a private setter. Deposits and withdrawals must use domain methods that reject non-positive amounts, fractional cents, and overdrafts. This keeps the money rules true regardless of which endpoint invokes them.

## Transaction history and transfers

Successful deposits and withdrawals create immutable `AccountTransaction` records with the resulting balance and UTC timestamp. A transfer creates a `TransferOut` record for the source account and a `TransferIn` record for the destination account.

Transfers run inside one explicit EF Core database transaction. The source withdrawal, destination deposit, both history records, and the save operation either commit together or roll back together. The transaction is executed through EF Core's execution strategy because SQL Server retry configuration requires the full transaction to be retried as one unit.

## Authentication and account ownership

ASP.NET Core Identity owns password hashing and the application cookie. Application code never stores or logs raw passwords. New users receive the `Customer` role. Account creation obtains its owner ID from the signed-in user's claim, never from a request body.

Customers may list and operate only on their own accounts. Administrators may access all accounts. A customer transfer requires ownership of both accounts; an administrator may transfer between any two accounts. Requests that cross a customer's ownership boundary return `403 Forbidden`.

Razor Pages render the browser workflow and use normal anti-forgery-protected form posts. Their handlers and the Minimal APIs share the focused `AccountOperationsService`, so account rules and persistence remain consistent without a second frontend framework or generic data layer.

## Error and operational conventions

The API uses built-in `ILogger` and ASP.NET Core Problem Details. Validation, missing resources, insufficient funds, unauthenticated requests, and forbidden requests use `application/problem+json` with a trace ID. GitHub Actions restores, builds, verifies formatting, tests, audits dependencies, builds the container, and stores coverage as an artifact without enforcing an artificial percentage target.
