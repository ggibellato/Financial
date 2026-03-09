namespace Financial.Application.DTO
{
    public class PortfolioDTO
    {
        public required string Name { get; set; }
        public required List<string> Assets { get; set; }
    }
}
