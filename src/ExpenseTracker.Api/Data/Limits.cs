// Size and paging limits, kept in one place so the schema and the validator can't drift apart.
namespace ExpenseTracker.Api.Data;

/// <summary>Validation limits shared by the database schema and the request validator.</summary>
public static class Limits
{
    public const int DescriptionMaxLength = 200;
    public const int CategoryMaxLength = 50;
    public const decimal MaxAmount = 1_000_000m;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}
