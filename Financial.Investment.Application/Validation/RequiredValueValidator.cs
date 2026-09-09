using System;

namespace Financial.Investment.Application.Validation;

internal static class RequiredValueValidator
{
    public static string Required(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{parameterName} is required.", parameterName)
            : value;
}
