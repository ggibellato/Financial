namespace Financial.Application.DTOs
{
    public class BrokerInfoDTO
    {
        public decimal TotalBought { get; set; }
        public decimal TotalSold { get; set; }
        public CreditInfoDTO TotalCredits { get; set; } = new CreditInfoDTO();
        public decimal TotalBoughtActive { get; set; }
        public decimal TotalSoldActive { get; set; }
        public CreditInfoDTO TotalCreditsActive { get; set; } = new CreditInfoDTO();
        public List<PortfolioDTO> PortfoliosActive { get; set; } = new List<PortfolioDTO>();
        public List<PortfolioDTO> PortfoliosInactive { get; set; } = new List<PortfolioDTO>();
    }
}

