using ExpenseTracker.Api.Data;

namespace ExpenseTracker.Api.Expenses;

// Request and response shapes. The API speaks decimal dollars; the database stores cents.

public sealed record ExpenseRequest(string? Description, decimal Amount, string? Category, DateOnly? SpentOn);

public sealed record ExpenseResponse(int Id, string Description, decimal Amount, string Category, DateOnly SpentOn)
{
    public static ExpenseResponse From(Expense e) =>
        new(e.Id, e.Description, Money.FromCents(e.AmountCents), e.Category, e.SpentOn);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record CategoryTotal(string Category, decimal Total, int Count);

public sealed record SummaryResponse(DateOnly From, DateOnly To, decimal Total, IReadOnlyList<CategoryTotal> ByCategory);
