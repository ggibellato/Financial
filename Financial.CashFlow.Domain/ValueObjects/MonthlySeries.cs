using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.CashFlow.Domain.ValueObjects;

/// <summary>
/// An immutable series of twelve monthly values (January through December) for a single,
/// externally-tracked year. The year itself is not part of this value object - callers already
/// know which year a series belongs to (a method parameter, a sibling DTO field), so keeping the
/// series year-agnostic lets it be reused for category totals, investment values, and income
/// figures alike.
/// </summary>
public sealed class MonthlySeries : IEquatable<MonthlySeries>
{
    public const int MonthsInYear = 12;

    private readonly decimal[] _values;

    private MonthlySeries(decimal[] values)
    {
        _values = values;
    }

    public static MonthlySeries Zero() => new(new decimal[MonthsInYear]);

    public static MonthlySeries FromMonthlyValues(IReadOnlyList<decimal> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Count != MonthsInYear)
        {
            throw new ArgumentException($"A monthly series requires exactly {MonthsInYear} values.", nameof(values));
        }

        return new MonthlySeries(values.ToArray());
    }

    public decimal this[int monthIndex] => _values[monthIndex];

    public decimal Sum() => _values.Sum();

    public decimal Average(int monthsElapsed, int decimalPlaces) =>
        monthsElapsed <= 0 ? 0m : Math.Round(Sum() / monthsElapsed, decimalPlaces);

    public IReadOnlyList<decimal?> DiffsFrom(decimal? priorClosingValue)
    {
        var diffs = new decimal?[MonthsInYear];
        diffs[0] = priorClosingValue.HasValue ? _values[0] - priorClosingValue.Value : null;

        for (var month = 1; month < MonthsInYear; month++)
        {
            diffs[month] = _values[month] - _values[month - 1];
        }

        return diffs;
    }

    public MonthlySeries Add(MonthlySeries other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        var summed = new decimal[MonthsInYear];
        for (var month = 0; month < MonthsInYear; month++)
        {
            summed[month] = _values[month] + other._values[month];
        }

        return new MonthlySeries(summed);
    }

    public IReadOnlyList<decimal> AsReadOnly() => _values.ToArray();

    public bool Equals(MonthlySeries? other) => other is not null && _values.SequenceEqual(other._values);

    public override bool Equals(object? obj) => Equals(obj as MonthlySeries);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _values)
        {
            hash.Add(value);
        }
        return hash.ToHashCode();
    }
}
