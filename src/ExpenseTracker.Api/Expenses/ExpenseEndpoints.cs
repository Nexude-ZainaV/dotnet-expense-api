using ExpenseTracker.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Expenses;

public static class ExpenseEndpoints
{
    public static RouteGroupBuilder MapExpenseEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/expenses").WithTags("Expenses");

        group.MapGet("/", List).WithName("ListExpenses");
        group.MapGet("/summary", Summary).WithName("ExpenseSummary");
        group.MapGet("/{id:int}", GetById).WithName("GetExpense");
        group.MapPost("/", Create).WithName("CreateExpense");
        group.MapPut("/{id:int}", Update).WithName("UpdateExpense");
        group.MapDelete("/{id:int}", Delete).WithName("DeleteExpense");

        return group;
    }

    // GET /api/expenses?category=food&from=2026-09-01&to=2026-09-30&page=1&pageSize=50
    private static async Task<Ok<PagedResponse<ExpenseResponse>>> List(
        ExpenseDbContext db, string? category, DateOnly? from, DateOnly? to,
        int page = 1, int pageSize = Limits.DefaultPageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Limits.MaxPageSize);

        var query = Filter(db.Expenses.AsNoTracking(), category, from, to);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.SpentOn).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<ExpenseResponse>(
            items.Select(ExpenseResponse.From).ToList(), page, pageSize, total));
    }

    // GET /api/expenses/summary?from=...&to=...  (defaults to the current month)
    private static async Task<Results<Ok<SummaryResponse>, ValidationProblem>> Summary(
        ExpenseDbContext db, TimeProvider clock, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var start = from ?? new DateOnly(today.Year, today.Month, 1);
        var end = to ?? start.AddMonths(1).AddDays(-1);
        if (end < start)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["to"] = ["'to' must be on or after 'from'."],
            });

        // GROUP BY runs in SQL; only one row per category comes back to the app.
        var rows = await Filter(db.Expenses.AsNoTracking(), null, start, end)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Cents = g.Sum(e => e.AmountCents), Count = g.Count() })
            .ToListAsync(ct);

        var byCategory = rows
            .OrderByDescending(r => r.Cents).ThenBy(r => r.Category)
            .Select(r => new CategoryTotal(r.Category, Money.FromCents(r.Cents), r.Count))
            .ToList();

        return TypedResults.Ok(new SummaryResponse(
            start, end, Money.FromCents(rows.Sum(r => r.Cents)), byCategory));
    }

    private static async Task<Results<Ok<ExpenseResponse>, NotFound>> GetById(
        int id, ExpenseDbContext db, CancellationToken ct)
    {
        var expense = await db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return expense is null ? TypedResults.NotFound() : TypedResults.Ok(ExpenseResponse.From(expense));
    }

    private static async Task<Results<Created<ExpenseResponse>, ValidationProblem>> Create(
        ExpenseRequest request, ExpenseDbContext db, CancellationToken ct)
    {
        var errors = ExpenseValidator.Validate(request);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var expense = new Expense
        {
            Description = request.Description!.Trim(),
            AmountCents = Money.ToCents(request.Amount),
            Category = ExpenseValidator.NormalizeCategory(request.Category!),
            SpentOn = request.SpentOn!.Value,
        };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/expenses/{expense.Id}", ExpenseResponse.From(expense));
    }

    private static async Task<Results<Ok<ExpenseResponse>, NotFound, ValidationProblem>> Update(
        int id, ExpenseRequest request, ExpenseDbContext db, CancellationToken ct)
    {
        var errors = ExpenseValidator.Validate(request);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var expense = await db.Expenses.FindAsync([id], ct);
        if (expense is null)
            return TypedResults.NotFound();

        expense.Description = request.Description!.Trim();
        expense.AmountCents = Money.ToCents(request.Amount);
        expense.Category = ExpenseValidator.NormalizeCategory(request.Category!);
        expense.SpentOn = request.SpentOn!.Value;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ExpenseResponse.From(expense));
    }

    private static async Task<Results<NoContent, NotFound>> Delete(int id, ExpenseDbContext db, CancellationToken ct)
    {
        // ExecuteDeleteAsync issues one DELETE statement without loading the row first.
        var deleted = await db.Expenses.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static IQueryable<Expense> Filter(IQueryable<Expense> query, string? category, DateOnly? from, DateOnly? to)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = ExpenseValidator.NormalizeCategory(category);
            query = query.Where(e => e.Category == normalized);
        }
        if (from is not null)
            query = query.Where(e => e.SpentOn >= from);
        if (to is not null)
            query = query.Where(e => e.SpentOn <= to);
        return query;
    }
}
