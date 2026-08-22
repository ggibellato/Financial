namespace Financial.Investment.Application.DTOs;

public class TransactionDTO
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public required string Type { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Fees { get; set; }

    public decimal TotalPrice { get; set; }
}
