namespace ExpenseTracker.Api.Data;

/// <summary>
/// One spending record. Money is stored as whole cents (long) instead of decimal:
/// integer arithmetic never rounds, and SQLite can sum and order integers natively.
/// </summary>
public sealed class Expense
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public long AmountCents { get; set; }
    public required string Category { get; set; }
    public DateOnly SpentOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
