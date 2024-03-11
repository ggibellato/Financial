namespace Financial.Application.DTO
{
    public class AssetInfoDTO
    {
        public string Ticker { get; set; }
        public string Exchange { get; set; }
        public decimal TotalBought { get; set; }
        public decimal TotalSold { get; set; }
        public CreditInfoDTO Credits { get; set; } = new CreditInfoDTO();
        public decimal Quantity { get; set; }
        public decimal AvaragePrice { get; set; }
        public decimal CurrentValue { get; set; }
        public Dictionary<DateOnly, decimal> InvestedHistory { get; set; } = new();
    }
}
