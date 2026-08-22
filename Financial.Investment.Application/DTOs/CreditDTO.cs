namespace Financial.Investment.Application.DTOs;

public class CreditDTO
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public required string Type { get; set; }

    public decimal Value { get; set; }
}

