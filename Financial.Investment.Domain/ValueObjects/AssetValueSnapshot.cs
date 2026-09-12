using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.ValueObjects;

public record AssetValueSnapshot(string Ticker, string Name, decimal Price, DateTimeOffset AsOf, PriceSource Source = PriceSource.Unknown);
