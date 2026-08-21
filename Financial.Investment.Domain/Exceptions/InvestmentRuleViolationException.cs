using System;

namespace Financial.Investment.Domain.Exceptions;

/// <summary>
/// Thrown when a request is well formed but the investment domain refuses it on its own rules -
/// moving an asset into the portfolio it already sits in, or into one that already holds an asset
/// by the same name.
/// </summary>
/// <remarks>
/// Its own type rather than <see cref="InvalidOperationException"/> because Infrastructure already
/// throws that for genuine upstream faults. Sharing the type would force the API to map real
/// defects and rule violations to the same status code.
/// </remarks>
public sealed class InvestmentRuleViolationException : Exception
{
    public InvestmentRuleViolationException(string message) : base(message)
    {
    }
}
