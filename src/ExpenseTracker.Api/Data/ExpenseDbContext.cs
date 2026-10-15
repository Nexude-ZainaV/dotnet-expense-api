// EF Core context: the Expenses table, its column lengths and its indexes.
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var expense = modelBuilder.Entity<Expense>();
        expense.Property(e => e.Description).HasMaxLength(Limits.DescriptionMaxLength);
        expense.Property(e => e.Category).HasMaxLength(Limits.CategoryMaxLength);
        // The list endpoint filters by category and date range, so index both.
        expense.HasIndex(e => new { e.Category, e.SpentOn });
        expense.HasIndex(e => e.SpentOn);
    }
}
