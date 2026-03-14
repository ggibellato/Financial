using System;
using System.Collections.Generic;

namespace Financial.Presentation.Tools.ViewModels;

public enum CreditDialogMode
{
    Add,
    Update,
    Delete
}

public sealed class CreditDialogViewModel : ViewModelBase
{
    private DateTime _date;
    private string _type = string.Empty;
    private decimal _value;
    private string _validationMessage = string.Empty;

    public CreditDialogMode Mode { get; }
    public Guid CreditId { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public string Title => Mode switch
    {
        CreditDialogMode.Add => "Add Credit",
        CreditDialogMode.Update => "Update Credit",
        CreditDialogMode.Delete => "Delete Credit",
        _ => "Credit"
    };

    public string ConfirmLabel => Mode switch
    {
        CreditDialogMode.Add => "Add",
        CreditDialogMode.Update => "Update",
        CreditDialogMode.Delete => "Delete",
        _ => "Confirm"
    };

    public bool IsReadOnly => Mode == CreditDialogMode.Delete;
    public bool IsEditable => !IsReadOnly;

    public DateTime Date
    {
        get => _date;
        set
        {
            if (SetProperty(ref _date, value))
            {
                Validate();
            }
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                Validate();
            }
        }
    }

    public decimal Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                Validate();
            }
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public CreditDialogViewModel(
        CreditDialogMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        Guid creditId,
        DateTime date,
        string type,
        decimal value)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        CreditId = creditId;

        _date = date;
        _type = type;
        _value = value;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public static CreditDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName)
    {
        return new CreditDialogViewModel(
            CreditDialogMode.Add,
            brokerName,
            portfolioName,
            assetName,
            Guid.Empty,
            DateTime.Today,
            "Dividend",
            0);
    }

    public static CreditDialogViewModel CreateForUpdate(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal value)
    {
        return new CreditDialogViewModel(
            CreditDialogMode.Update,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            value);
    }

    public static CreditDialogViewModel CreateForDelete(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal value)
    {
        return new CreditDialogViewModel(
            CreditDialogMode.Delete,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            value);
    }

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    private bool CanConfirm()
    {
        if (Mode == CreditDialogMode.Delete)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(ValidationMessage);
    }

    private void Validate()
    {
        if (Mode == CreditDialogMode.Delete)
        {
            ValidationMessage = string.Empty;
            ConfirmCommand.RaiseCanExecuteChanged();
            return;
        }

        var errors = new List<string>();

        if (Date == DateTime.MinValue)
        {
            errors.Add("Date is required.");
        }

        if (!IsValidCreditType(Type))
        {
            errors.Add("Type must be Dividend or Rent.");
        }

        if (Value <= 0)
        {
            errors.Add("Value must be greater than zero.");
        }

        ValidationMessage = string.Join(Environment.NewLine, errors);
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    private static bool IsValidCreditType(string? value)
    {
        return string.Equals(value, "Dividend", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Rent", StringComparison.OrdinalIgnoreCase);
    }
}
