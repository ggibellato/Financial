namespace Financial.Application.DTO
{
    public class BrokerInfoDTO
    {
        public decimal TotalBought { get; set; }
        public decimal TotalSold { get; set; }
        public CreditInfoDTO TotalCredits { get; set; } = new CreditInfoDTO();
        public decimal TotalBoughtActive { get; set; }
        public decimal TotalSoldActive { get; set; }
        public CreditInfoDTO TotalCreditsActive { get; set; } = new CreditInfoDTO();
        public List<PortifolioDTO> PortifiliosActive { get; set; } = new List<PortifolioDTO>();
        public List<PortifolioDTO> PortifiliosInactive { get; set; } = new List<PortifolioDTO>();
    }
}
