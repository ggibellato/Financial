using System;

namespace Financial.Investment.Domain.ValueObjects;

public record DividendValue(DividendType Type, DateTime Date, decimal Value);
