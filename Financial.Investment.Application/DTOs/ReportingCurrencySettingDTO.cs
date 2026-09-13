namespace Financial.Investment.Application.DTOs;

public class ReportingCurrencySettingDTO
{
    public required string Currency { get; set; }
    public bool Enabled { get; set; } = true;
}
