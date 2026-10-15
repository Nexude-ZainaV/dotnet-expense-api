// HTTP-level tests for every expense endpoint. Run with: dotnet test
using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Expenses;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Tests;

// xUnit creates a new instance of this class per test, so every test gets its own
// factory and database. Dispose tears them down afterwards.
public sealed class ExpenseEndpointTests : IDisposable
{
    private readonly ExpenseApiFactory _factory = new();
    private readonly HttpClient _client;

    public ExpenseEndpointTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // Seeds one expense through the API and returns what it created.
    private async Task<ExpenseResponse> CreateAsync(string description, decimal amount, string category, string spentOn)
    {
        var response = await _client.PostAsJsonAsync("/api/expenses",
            new ExpenseRequest(description, amount, category, DateOnly.Parse(spentOn)));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExpenseResponse>())!;
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_201_with_location_and_normalized_category()
    {
        var response = await _client.PostAsJsonAsync("/api/expenses",
            new ExpenseRequest("  Groceries ", 42.5m, " Food ", new DateOnly(2026, 9, 3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.NotNull(body);
        Assert.Equal($"/api/expenses/{body.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Groceries", body.Description);
        Assert.Equal("food", body.Category);
        Assert.Equal(42.50m, body.Amount);
    }

    [Theory]
    [InlineData("", 10, "food", "description")]
    [InlineData("Taxi", 0, "travel", "amount")]
    [InlineData("Taxi", -5, "travel", "amount")]
    [InlineData("Taxi", 10.123, "travel", "amount")]
    [InlineData("Taxi", 10, " ", "category")]
    public async Task Create_rejects_invalid_input_with_validation_problem(
        string description, decimal amount, string category, string invalidField)
    {
        var response = await _client.PostAsJsonAsync("/api/expenses",
            new ExpenseRequest(description, amount, category, new DateOnly(2026, 9, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(invalidField, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var response = await _client.GetAsync("/api/expenses/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_record()
    {
        var created = await CreateAsync("Coffee", 3.5m, "food", "2026-09-02");

        var response = await _client.PutAsJsonAsync($"/api/expenses/{created.Id}",
            new ExpenseRequest("Coffee beans", 18m, "food", new DateOnly(2026, 9, 2)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<ExpenseResponse>($"/api/expenses/{created.Id}");
        Assert.Equal("Coffee beans", fetched!.Description);
        Assert.Equal(18m, fetched.Amount);
    }

    [Fact]
    public async Task Delete_removes_the_record_then_404s()
    {
        var created = await CreateAsync("Movie", 12m, "fun", "2026-09-05");

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/expenses/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/expenses/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task List_filters_by_category_and_date_and_pages_newest_first()
    {
        await CreateAsync("Rent", 1200m, "housing", "2026-09-01");
        await CreateAsync("Lunch", 11m, "food", "2026-09-10");
        await CreateAsync("Dinner", 30m, "food", "2026-09-12");
        await CreateAsync("Old lunch", 9m, "food", "2026-08-20");

        var page = await _client.GetFromJsonAsync<PagedResponse<ExpenseResponse>>(
            "/api/expenses?category=FOOD&from=2026-09-01&to=2026-09-30&pageSize=1");

        Assert.Equal(2, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Dinner", page.Items[0].Description);   // newest first
    }

    [Fact]
    public async Task Summary_defaults_to_current_month_and_totals_by_category()
    {
        await CreateAsync("Rent", 1200m, "housing", "2026-09-01");
        await CreateAsync("Lunch", 11.10m, "food", "2026-09-10");
        await CreateAsync("Dinner", 30.20m, "food", "2026-09-12");
        await CreateAsync("August", 500m, "housing", "2026-08-31");   // outside the month

        var summary = await _client.GetFromJsonAsync<SummaryResponse>("/api/expenses/summary");

        Assert.Equal(new DateOnly(2026, 9, 1), summary!.From);
        Assert.Equal(new DateOnly(2026, 9, 30), summary.To);
        Assert.Equal(1241.30m, summary.Total);
        Assert.Equal(["housing", "food"], summary.ByCategory.Select(c => c.Category));
        Assert.Equal(41.30m, summary.ByCategory[1].Total);
        Assert.Equal(2, summary.ByCategory[1].Count);
    }

    [Fact]
    public async Task Summary_rejects_reversed_range()
    {
        var response = await _client.GetAsync("/api/expenses/summary?from=2026-09-30&to=2026-09-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
