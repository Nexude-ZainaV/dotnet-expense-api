# Expense Tracker API (ASP.NET Core, .NET 10)

[![CI](https://github.com/Nexude-ZainaV/dotnet-expense-api/actions/workflows/ci.yml/badge.svg)](https://github.com/Nexude-ZainaV/dotnet-expense-api/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-SQLite-003B57?logo=sqlite)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

A REST API for recording expenses and reporting monthly totals per category. Built
with ASP.NET Core minimal APIs and Entity Framework Core on SQLite, covered by
in-memory integration tests that exercise the real HTTP pipeline.

## Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/expenses?category=&from=&to=&page=&pageSize=` | Filtered, paged list, newest first |
| `GET` | `/api/expenses/{id}` | One expense, or `404` |
| `POST` | `/api/expenses` | Create; `201 Created` with a `Location` header |
| `PUT` | `/api/expenses/{id}` | Replace; `404` if missing |
| `DELETE` | `/api/expenses/{id}` | `204 No Content`, or `404` |
| `GET` | `/api/expenses/summary?from=&to=` | Totals per category (defaults to the current month) |
| `GET` | `/health` | Liveness check |
| `GET` | `/openapi/v1.json` | OpenAPI document (Development only) |

Invalid input returns `400` with an RFC 9457 `ValidationProblemDetails` body:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "amount": ["Amount can have at most 2 decimal places."] }
}
```

Ready-made requests are in [docs/expenses.http](docs/expenses.http).

## Design decisions

- **Money is stored as integer cents.** `decimal` in the API, `long` in the database:
  no floating-point drift, and SQLite sums and sorts integers natively.
  Conversion rounds half away from zero (`Money.ToCents`).
- **Aggregation happens in SQL.** The summary uses `GroupBy` + `Sum`, which EF Core
  translates to `GROUP BY`, so only one row per category leaves the database.
- **Typed results** (`Results<Ok<T>, NotFound, ValidationProblem>`) make every
  endpoint's possible responses visible in its signature and in the OpenAPI document.
- **Injectable clock.** `TimeProvider` is registered in DI, so tests pin "today" and
  the "current month" default is deterministic.
- **Indexes match the queries**: `(Category, SpentOn)` and `SpentOn`.
- `ExecuteDeleteAsync` deletes with one SQL statement instead of load-then-remove.
- The schema is created with `EnsureCreated()` to keep the demo one command to run;
  a production service would ship EF Core migrations.

## Run it

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ExpenseTracker.Api
# listening on http://localhost:5082
```

Tests (integration tests use `WebApplicationFactory` and a throwaway SQLite file per test):

```bash
dotnet test
```

## Project layout

```
src/ExpenseTracker.Api/
  Program.cs              composition root: DI, middleware, route mapping
  Data/                   entity, DbContext, shared limits
  Expenses/               endpoints, request/response contracts, validation, money helpers
tests/ExpenseTracker.Api.Tests/
  ExpenseApiFactory.cs    boots the API in memory with an isolated database and fixed clock
  ExpenseEndpointTests.cs HTTP-level tests for every endpoint
  MoneyTests.cs           unit tests for cents conversion
docs/expenses.http        sample requests
```

## License

[MIT](LICENSE)
