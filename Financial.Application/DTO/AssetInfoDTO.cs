namespace Financial.Application.DTO
{
    public class AssetInfoDTO
    {
        public decimal TotalBought { get; set; }
        public decimal TotalSold { get; set; }
        public CreditInfoDTO Credits { get; set; } = new CreditInfoDTO();
        public decimal Quantity { get; set; }
    }
}
