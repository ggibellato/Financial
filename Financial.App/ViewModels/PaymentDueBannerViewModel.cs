using System.Windows.Threading;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Reads F01's payments-due list directly from the in-process <see cref="IPaymentsDueService"/>
/// (no HTTP call, since Financial.App hosts CashFlow's Application layer in-process - see
/// SyncStatusViewModel for the same pattern), once at construction. The service already fails
/// safe internally (catches and logs, returns an empty list), so no additional error handling is
/// needed here.
/// </summary>
public class PaymentDueBannerViewModel : ViewModelBase
{
    public static readonly TimeSpan DismissDelay = TimeSpan.FromSeconds(10);

    private readonly IPaymentsDueService _paymentsDueService;
    private readonly DispatcherTimer _dismissTimer;
    private bool _isVisible;

    public PaymentDueBannerViewModel(IPaymentsDueService paymentsDueService)
    {
        _paymentsDueService = paymentsDueService ?? throw new ArgumentNullException(nameof(paymentsDueService));

        Payments = _paymentsDueService.GetPaymentsDue()
            .Select(payment => new PaymentDueRowViewModel(payment))
            .ToList();

        _isVisible = Payments.Count > 0;

        DismissCommand = new RelayCommand(Dismiss);

        _dismissTimer = new DispatcherTimer { Interval = DismissDelay };
        _dismissTimer.Tick += (_, _) => Dismiss();

        if (_isVisible)
        {
            _dismissTimer.Start();
        }
    }

    public IReadOnlyList<PaymentDueRowViewModel> Payments { get; }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public RelayCommand DismissCommand { get; }

    public void Dismiss()
    {
        _dismissTimer.Stop();
        IsVisible = false;
    }
}
