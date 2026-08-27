using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class WithdrawalViewModel : ViewModelBase
{
    private readonly IReserveService _reserveService;
    private readonly Func<string, bool> _confirm;
    private readonly Action _closeOtherForms;
    private readonly Func<Task> _refresh;

    private bool _isWithdrawalFormOpen;
    private Guid? _withdrawalBucketId;
    private string _withdrawalAmount = string.Empty;
    private DateTime? _withdrawalDate;
    private string _withdrawalDescription = string.Empty;
    private bool _isSubmittingWithdrawal;
    private string? _withdrawalSaveError;

    /// <summary>The same instance ReservaViewModel owns — mutated in place by its refresh, never replaced.</summary>
    public ObservableCollection<ReserveBucketDTO> Buckets { get; }

    public bool IsWithdrawalFormOpen
    {
        get => _isWithdrawalFormOpen;
        private set => SetProperty(ref _isWithdrawalFormOpen, value);
    }

    public Guid? WithdrawalBucketId
    {
        get => _withdrawalBucketId;
        set => SetProperty(ref _withdrawalBucketId, value);
    }

    public string WithdrawalAmount
    {
        get => _withdrawalAmount;
        set => SetProperty(ref _withdrawalAmount, value);
    }

    public DateTime? WithdrawalDate
    {
        get => _withdrawalDate;
        set => SetProperty(ref _withdrawalDate, value);
    }

    public string WithdrawalDescription
    {
        get => _withdrawalDescription;
        set => SetProperty(ref _withdrawalDescription, value);
    }

    public bool IsSubmittingWithdrawal
    {
        get => _isSubmittingWithdrawal;
        private set => SetProperty(ref _isSubmittingWithdrawal, value);
    }

    public string? WithdrawalSaveError
    {
        get => _withdrawalSaveError;
        private set => SetProperty(ref _withdrawalSaveError, value);
    }

    public RelayCommand ShowWithdrawalFormCommand { get; }
    public RelayCommand CancelWithdrawalFormCommand { get; }
    public RelayCommand SubmitWithdrawalCommand { get; }

    public WithdrawalViewModel(
        IReserveService reserveService, ObservableCollection<ReserveBucketDTO> buckets,
        Func<string, bool> confirm, Action closeOtherForms, Func<Task> refresh)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
        Buckets = buckets ?? throw new ArgumentNullException(nameof(buckets));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _closeOtherForms = closeOtherForms ?? throw new ArgumentNullException(nameof(closeOtherForms));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowWithdrawalFormCommand = new RelayCommand(ShowWithdrawalForm);
        CancelWithdrawalFormCommand = new RelayCommand(CloseWithdrawalForm);
        SubmitWithdrawalCommand = new RelayCommand(async () => await SubmitWithdrawalAsync());
    }

    internal Guid? DefaultBucketId() =>
        (Buckets.Where(b => b.IsActive).FirstOrDefault() ?? Buckets.FirstOrDefault())?.Id;

    internal void ShowWithdrawalForm()
    {
        _closeOtherForms();
        WithdrawalBucketId = DefaultBucketId();
        WithdrawalAmount = string.Empty;
        WithdrawalDate = DateTime.Today;
        WithdrawalDescription = string.Empty;
        WithdrawalSaveError = null;
        IsWithdrawalFormOpen = true;
    }

    internal void CloseWithdrawalForm()
    {
        IsWithdrawalFormOpen = false;
        WithdrawalSaveError = null;
    }

    internal Task SubmitWithdrawalAsync() => ExecuteSaveAsync(
        () => WithdrawalFormValidation.BuildValidationMessage(WithdrawalBucketId, WithdrawalAmount, WithdrawalDate, WithdrawalDescription),
        error => WithdrawalSaveError = error,
        saving => IsSubmittingWithdrawal = saving,
        async () =>
        {
            await PostWithdrawalWithOverdraftHandlingAsync(confirmed: false);
            CloseWithdrawalForm();
            await _refresh();
        });

    /// <summary>
    /// Posts the withdrawal; on an overdraft conflict, asks the user to confirm and resubmits
    /// with the override flag set. Declining re-throws the server's conflict message so the
    /// caller's catch block surfaces it as WithdrawalSaveError. Mirrors useReserva.ts's
    /// ApiError(409) + window.confirm flow.
    /// </summary>
    private async Task PostWithdrawalWithOverdraftHandlingAsync(bool confirmed)
    {
        var request = new WithdrawalRequestDTO
        {
            BucketId = WithdrawalBucketId!.Value,
            Amount = decimal.Parse(WithdrawalAmount),
            Date = DateOnly.FromDateTime(WithdrawalDate!.Value),
            Description = WithdrawalDescription,
            Confirmed = confirmed,
        };

        try
        {
            await _reserveService.PostWithdrawalAsync(request);
        }
        catch (OverdraftConfirmationRequiredException ex) when (!confirmed)
        {
            if (!_confirm($"{ex.Message}\n\nProceed anyway?"))
            {
                throw;
            }

            await PostWithdrawalWithOverdraftHandlingAsync(confirmed: true);
        }
    }
}
