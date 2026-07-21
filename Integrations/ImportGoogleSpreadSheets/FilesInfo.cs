using System.ComponentModel;

namespace Financial.Investment.Infrastructure.Integrations.ImportGoogleSpreadSheets;

public class FilesInfo : INotifyPropertyChanged
{
    public string Id { get; set; }
    public string Name { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
