using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ICreditService
{
    AssetDetailsDTO? AddCredit(CreditCreateDTO request);
    AssetDetailsDTO? UpdateCredit(CreditUpdateDTO request);
    AssetDetailsDTO? DeleteCredit(CreditDeleteDTO request);
}
