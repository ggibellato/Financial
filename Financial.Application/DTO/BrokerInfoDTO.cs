namespace Financial.Application.DTO
{
    public class BrokerInfoDTO
    {
        public decimal TotalBought { get; set; }
        public decimal TotalSold { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalCreditsActive { get; set; }
        public decimal TotalBoughtActive { get; set; }
        public decimal TotalSoldActive { get; set; }
        public List<string> AssetsActive { get; set; } = new List<string>();
        public List<string> AssetsInactive { get; set; } = new List<string>();
    }
}
