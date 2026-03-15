using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IOperationService
{
    AssetDetailsDTO? AddOperation(OperationCreateDTO request);
    AssetDetailsDTO? UpdateOperation(OperationUpdateDTO request);
    AssetDetailsDTO? DeleteOperation(OperationDeleteDTO request);
}
