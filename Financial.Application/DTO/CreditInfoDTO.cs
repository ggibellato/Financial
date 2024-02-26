namespace Financial.Application.DTO
{
    public class CreditInfoDTO
    {
        public decimal Total { get; set; }
        public Dictionary<DateOnly, decimal> CreditsByMonth { get; set; } = new Dictionary<DateOnly, decimal>();
    }
}
