using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels.Investment;

public sealed class LotAllocationRowViewModel : ViewModelBase
{
    private decimal _quantity;

    public LotAllocationRowViewModel(OpenLotDTO lot)
    {
        SourceTransactionId = lot.SourceTransactionId;
        Date = lot.Date;
        RemainingQuantity = lot.RemainingQuantity;
        UnitCost = lot.UnitCost;
    }

    public Guid SourceTransactionId { get; }
    public DateTime Date { get; }
    public decimal RemainingQuantity { get; }
    public decimal UnitCost { get; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(IsOverAllocated));
            }
        }
    }

    public bool IsOverAllocated => LotAllocationCalculator.IsLotOverAllocated(this);
}
