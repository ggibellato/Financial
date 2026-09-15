using System;

namespace Financial.Investment.Application.DTOs;

public class TaxRuleUpdateDTO
{
    public required string Label { get; set; }

    public string Description { get; set; } = string.Empty;

    public required DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
