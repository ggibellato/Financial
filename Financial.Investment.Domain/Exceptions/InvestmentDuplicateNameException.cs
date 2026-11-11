namespace Financial.Investment.Domain.Exceptions;

public sealed class InvestmentDuplicateNameException : InvestmentRuleViolationException
{
    public InvestmentDuplicateNameException(string message) : base(message)
    {
    }
}
