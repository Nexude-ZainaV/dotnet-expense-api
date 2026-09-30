using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Expenses;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExpenseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Expenses")
                      ?? "Data Source=expenses.db"));
builder.Services.AddSingleton(TimeProvider.System);   // injectable clock, so tests can pin "today"
builder.Services.AddProblemDetails();                  // RFC 9457 error bodies for unhandled errors
builder.Services.AddOpenApi();

var app = builder.Build();

// Schema bootstrap for a single-file demo database. A multi-environment deployment
// would use EF Core migrations (dotnet ef migrations add ...) instead.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ExpenseDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // GET /openapi/v1.json
}

app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).WithTags("Health");
app.MapExpenseEndpoints();

app.Run();

// Makes the implicit Program class visible to WebApplicationFactory<Program> in the tests.
public partial class Program;
