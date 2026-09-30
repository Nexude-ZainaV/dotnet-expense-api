using ExpenseTracker.Api.Data;

namespace ExpenseTracker.Api.Expenses;

/// <summary>
/// Returns field errors in the shape ValidationProblem expects (field name -> messages).
/// An empty dictionary means the request is valid.
/// </summary>
public static class ExpenseValidator
{
    public static Dictionary<string, string[]> Validate(ExpenseRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Description))
            errors["description"] = ["Description is required."];
        else if (request.Description.Trim().Length > Limits.DescriptionMaxLength)
            errors["description"] = [$"Description must be at most {Limits.DescriptionMaxLength} characters."];

        if (request.Amount <= 0 || request.Amount > Limits.MaxAmount)
            errors["amount"] = [$"Amount must be greater than 0 and at most {Limits.MaxAmount:N0}."];
        else if (decimal.Round(request.Amount, 2) != request.Amount)
            errors["amount"] = ["Amount can have at most 2 decimal places."];

        if (string.IsNullOrWhiteSpace(request.Category))
            errors["category"] = ["Category is required."];
        else if (request.Category.Trim().Length > Limits.CategoryMaxLength)
            errors["category"] = [$"Category must be at most {Limits.CategoryMaxLength} characters."];

        if (request.SpentOn is null)
            errors["spentOn"] = ["SpentOn is required (yyyy-MM-dd)."];

        return errors;
    }

    // Categories are compared case-insensitively, so store one canonical spelling.
    public static string NormalizeCategory(string category) => category.Trim().ToLowerInvariant();
}
