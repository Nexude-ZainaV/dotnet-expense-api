using ExpenseTracker.Api.Expenses;

namespace ExpenseTracker.Api.Tests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("0.01", 1)]
    [InlineData("19.99", 1999)]
    [InlineData("0.125", 13)]    // half rounds away from zero, not to even
    [InlineData("1000000", 100000000)]
    public void ToCents_converts_dollars_to_whole_cents(string amount, long expected) =>
        Assert.Equal(expected, Money.ToCents(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void FromCents_round_trips() => Assert.Equal(19.99m, Money.FromCents(Money.ToCents(19.99m)));
}
