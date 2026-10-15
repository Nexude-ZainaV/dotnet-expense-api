// Converts between decimal dollars (API) and whole cents (database).
namespace ExpenseTracker.Api.Expenses;

public static class Money
{
    // Banker's rounding would turn 0.125 into 0.12; money rounds half away from zero.
    public static long ToCents(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public static decimal FromCents(long cents) => cents / 100m;
}
