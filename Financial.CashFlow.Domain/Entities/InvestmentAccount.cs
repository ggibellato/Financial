using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.CashFlow.Domain.Entities;

public class InvestmentAccount
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsLiability { get; private set; }

    private List<string> _aliases = new List<string>();
    public IReadOnlyCollection<string> Aliases { get => _aliases.AsReadOnly(); private set => SetAliases(value); }
    private void SetAliases(IReadOnlyCollection<string> data)
    {
        _aliases.Clear();
        _aliases.AddRange(data);
    }

    private InvestmentAccount() { }

    public static InvestmentAccount Create(string name, bool isActive, bool isLiability)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            IsLiability = isLiability
        };
    }

    public void AddAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException("Alias must not be empty.");
        }

        if (_aliases.Any(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _aliases.Add(alias);
    }
}
