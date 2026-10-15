# Expense Tracker API (ASP.NET Core, .NET 10)

[![CI](https://github.com/Nexude-ZainaV/dotnet-expense-api/actions/workflows/ci.yml/badge.svg)](https://github.com/Nexude-ZainaV/dotnet-expense-api/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-SQLite-003B57?logo=sqlite)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

A REST API for recording expenses and getting monthly totals per category. It uses
ASP.NET Core minimal APIs and Entity Framework Core on SQLite. The tests boot the
whole app in memory and go through the real HTTP pipeline.

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

Bad input gets a `400` with an RFC 9457 `ValidationProblemDetails` body:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "amount": ["Amount can have at most 2 decimal places."] }
}
```

There are ready-made requests in [docs/expenses.http](docs/expenses.http).

## Design decisions

- Money is stored as integer cents: `decimal` in the API, `long` in the database.
  That avoids floating-point drift, and SQLite can sum and sort integers natively.
  Conversion rounds half away from zero (`Money.ToCents`).
- The summary is aggregated in SQL. `GroupBy` + `Sum` turns into a `GROUP BY`, so
  only one row per category comes back from the database.
- I used typed results (`Results<Ok<T>, NotFound, ValidationProblem>`) so each
  endpoint's possible responses show up in its signature and in the OpenAPI document.
- The clock is injectable. `TimeProvider` is registered in DI, which lets the tests
  pin "today" and makes the "current month" default deterministic.
- The indexes match the queries: `(Category, SpentOn)` and `SpentOn`.
- Delete uses `ExecuteDeleteAsync`, which is one SQL statement instead of
  load-then-remove.
- The schema comes from `EnsureCreated()` so the demo runs with one command. A real
  service would ship EF Core migrations instead.

## Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ExpenseTracker.Api
# listening on http://localhost:5082
```

Tests (the integration tests use `WebApplicationFactory` and a throwaway SQLite file per test):

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
