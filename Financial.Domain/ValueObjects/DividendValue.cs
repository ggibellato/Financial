using System;

namespace Financial.Domain.ValueObjects;

public record DividendValue(DividendType Type, DateTime Date, decimal Value);
