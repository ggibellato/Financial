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
        public List<PortifolioDTO> PortifiliosActive { get; set; } = new List<PortifolioDTO>();
        public List<PortifolioDTO> PortifiliosInactive { get; set; } = new List<PortifolioDTO>();
    }
}
