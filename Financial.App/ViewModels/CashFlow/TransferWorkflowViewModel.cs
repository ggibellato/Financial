using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class TransferWorkflowViewModel : ViewModelBase
{
    private readonly ITransferService _transferService;
    private readonly Func<Task> _refresh;

    private bool _isTransferFormOpen;
    private Guid? _editingTransferId;
    private DateTime? _transferFormDate;
    private Guid? _transferFormSourceBank;
    private Guid? _transferFormDestinationBank;
    private string _transferFormAmount = string.Empty;
    private string _transferFormNote = string.Empty;
    private bool _isSavingTransfer;
    private string? _transferSaveError;

    /// <summary>The same instance MonthlyViewModel owns — mutated in place by its refresh, never replaced.</summary>
    public ObservableCollection<BankDTO> Banks { get; }

    public bool IsTransferFormOpen
    {
        get => _isTransferFormOpen;
        private set => SetProperty(ref _isTransferFormOpen, value);
    }

    public bool IsEditingTransfer => _editingTransferId != null;

    public DateTime? TransferFormDate
    {
        get => _transferFormDate;
        set => SetProperty(ref _transferFormDate, value);
    }

    public Guid? TransferFormSourceBank
    {
        get => _transferFormSourceBank;
        set
        {
            if (SetProperty(ref _transferFormSourceBank, value))
            {
                OnPropertyChanged(nameof(IsSameBankTransfer));
                OnPropertyChanged(nameof(SameBankTransferError));
                SaveTransferCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public Guid? TransferFormDestinationBank
    {
        get => _transferFormDestinationBank;
        set
        {
            if (SetProperty(ref _transferFormDestinationBank, value))
            {
                OnPropertyChanged(nameof(IsSameBankTransfer));
                OnPropertyChanged(nameof(SameBankTransferError));
                SaveTransferCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>True when source and destination are both set and identical — Move Money's Confirm is disabled in this state, mirroring TransferForm.tsx's sameBankError.</summary>
    public bool IsSameBankTransfer =>
        TransferFormSourceBank.HasValue
        && TransferFormDestinationBank.HasValue
        && TransferFormSourceBank == TransferFormDestinationBank;

    public string SameBankTransferError => IsSameBankTransfer ? "Source and destination must be different banks." : string.Empty;

    public string TransferFormAmount
    {
        get => _transferFormAmount;
        set => SetProperty(ref _transferFormAmount, value);
    }

    public string TransferFormNote
    {
        get => _transferFormNote;
        set => SetProperty(ref _transferFormNote, value);
    }

    public bool IsSavingTransfer
    {
        get => _isSavingTransfer;
        private set => SetProperty(ref _isSavingTransfer, value);
    }

    public string? TransferSaveError
    {
        get => _transferSaveError;
        private set
        {
            if (SetProperty(ref _transferSaveError, value))
            {
                OnPropertyChanged(nameof(DateFieldError));
                OnPropertyChanged(nameof(SourceBankFieldError));
                OnPropertyChanged(nameof(DestinationBankFieldError));
                OnPropertyChanged(nameof(AmountFieldError));
            }
        }
    }

    /// <summary>
    /// Per-field validation errors (P38-F05) — same substring-match pattern as F02's
    /// <c>AdjustmentWorkflowViewModel.TargetBalanceFieldError</c>, matching this form's own
    /// client-side <see cref="TransferFormValidation"/> text. Additive to
    /// <see cref="TransferSaveError"/>'s existing bottom-of-form message.
    /// </summary>
    public string? DateFieldError => MatchFieldError("Date is required.");

    public string? SourceBankFieldError => MatchFieldError("Source bank is required.");

    public string? DestinationBankFieldError =>
        MatchFieldError("Destination bank is required.", "Source and destination must be different banks.");

    public string? AmountFieldError => MatchFieldError("Amount must be a positive number.");

    private string? MatchFieldError(params string[] fragments) =>
        TransferSaveError is { } error && fragments.Any(f => error.Contains(f, StringComparison.OrdinalIgnoreCase))
            ? error
            : null;

    public RelayCommand<Guid?> ShowMoveMoneyFormCommand { get; }
    public RelayCommand CancelTransferFormCommand { get; }
    public RelayCommand SaveTransferCommand { get; }
    public RelayCommand<TransferDTO> EditTransferCommand { get; }

    public TransferWorkflowViewModel(ITransferService transferService, ObservableCollection<BankDTO> banks, Func<Task> refresh)
    {
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        Banks = banks ?? throw new ArgumentNullException(nameof(banks));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowMoveMoneyFormCommand = new RelayCommand<Guid?>(ShowCreateTransferForm);
        CancelTransferFormCommand = new RelayCommand(CloseTransferForm);
        SaveTransferCommand = new RelayCommand(async () => await SaveTransferAsync(), () => !IsSavingTransfer && !IsSameBankTransfer);
        EditTransferCommand = new RelayCommand<TransferDTO>(ShowEditTransferForm);
    }

    private void ShowCreateTransferForm(Guid? sourceBank)
    {
        _editingTransferId = null;
        TransferFormDate = DateTime.Today;
        TransferFormSourceBank = sourceBank ?? (Banks.Count > 0 ? Banks[0].Id : null);
        TransferFormDestinationBank = null;
        TransferFormAmount = string.Empty;
        TransferFormNote = string.Empty;
        TransferSaveError = null;
        OnPropertyChanged(nameof(IsEditingTransfer));
        IsTransferFormOpen = true;
    }

    private void ShowEditTransferForm(TransferDTO? transfer)
    {
        if (transfer is null)
        {
            return;
        }

        _editingTransferId = transfer.Id;
        TransferFormDate = transfer.Date.ToDateTime(TimeOnly.MinValue);
        TransferFormSourceBank = transfer.SourceBankId;
        TransferFormDestinationBank = transfer.DestinationBankId;
        TransferFormAmount = transfer.Amount.ToString("0.##");
        TransferFormNote = transfer.Note ?? string.Empty;
        TransferSaveError = null;
        OnPropertyChanged(nameof(IsEditingTransfer));
        IsTransferFormOpen = true;
    }

    private void CloseTransferForm()
    {
        IsTransferFormOpen = false;
        _editingTransferId = null;
        TransferSaveError = null;
    }

    internal Task SaveTransferAsync() => ExecuteSaveAsync(
        () => TransferFormValidation.BuildValidationMessage(
            TransferFormDate, TransferFormSourceBank, TransferFormDestinationBank, TransferFormAmount),
        error => TransferSaveError = error,
        saving => IsSavingTransfer = saving,
        async () =>
        {
            var date = DateOnly.FromDateTime(TransferFormDate!.Value);
            var amount = decimal.Parse(TransferFormAmount);
            var note = string.IsNullOrWhiteSpace(TransferFormNote) ? null : TransferFormNote;

            if (_editingTransferId is { } id)
            {
                await _transferService.UpdateTransferAsync(id, new TransferUpdateDTO
                {
                    Date = date, SourceBankId = TransferFormSourceBank!.Value,
                    DestinationBankId = TransferFormDestinationBank!.Value,
                    Amount = amount, Note = note,
                });
            }
            else
            {
                await _transferService.AddTransferAsync(new TransferCreateDTO
                {
                    Date = date, SourceBankId = TransferFormSourceBank!.Value,
                    DestinationBankId = TransferFormDestinationBank!.Value,
                    Amount = amount, Note = note,
                });
            }

            CloseTransferForm();
            await _refresh();
        },
        SaveTransferCommand.RaiseCanExecuteChanged);
}
