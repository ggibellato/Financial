namespace Financial.Investment.Application.DTOs;

public sealed record UpcomingIncomeDTO(
    string AssetName,
    string BrokerName,
    DateTime LastCreditDate,
    DateTime ProjectedNextDate,
    decimal ProjectedAmount);
