namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to post a month's income and trigger the automated Dizimo/Limpo split.
/// </summary>
public sealed class IncomeSplitRequestDTO
{
    /// <summary>Date to post the resulting movements under.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Gleison's gross salary. Display/record only - not used in the split math.</summary>
    public required decimal GleisonSalaryGross { get; init; }

    /// <summary>Gleison's net-of-tax salary.</summary>
    public required decimal GleisonSalaryNet { get; init; }

    /// <summary>Ariana's gross salary. Display/record only - not used in the split math.</summary>
    public required decimal ArianaSalaryGross { get; init; }

    /// <summary>Ariana's net-of-tax salary.</summary>
    public required decimal ArianaSalaryNet { get; init; }

    /// <summary>Gross Lottery income.</summary>
    public required decimal Lottery { get; init; }

    /// <summary>Gross Dividendo/Juros income.</summary>
    public required decimal DividendoJuros { get; init; }
}
