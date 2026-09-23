namespace Financial.Investment.Application.DTOs;

public class CorporateActionSpinOffResultDTO
{
    public AssetDetailsDTO? Parent { get; set; }
    public AssetDetailsDTO? New { get; set; }
}
