namespace Financial.CashFlow.Application.Validation;

public static class DescriptionValidator
{
    private const int DefaultMaxLength = 200;

    public static void EnsureWithinLimit(string? description, int maxLength = DefaultMaxLength)
    {
        if (description is not null && description.Length > maxLength)
        {
            throw new ArgumentException($"Description must not exceed {maxLength} characters.");
        }
    }
}
