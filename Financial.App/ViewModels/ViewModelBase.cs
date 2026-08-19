using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Base class for all ViewModels, providing INotifyPropertyChanged implementation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    /// <param name="propertyName">Name of the property that changed (auto-filled by compiler)</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value has changed
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <param name="field">Reference to the backing field</param>
    /// <param name="value">New value to set</param>
    /// <param name="propertyName">Name of the property (auto-filled by compiler)</param>
    /// <returns>True if the value was changed, false otherwise</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Runs the validate/save/error-handling sequence shared by every form-save command:
    /// validate, bail out with an error if invalid, else set the saving flag, clear the error,
    /// run <paramref name="save"/>, and report any exception's message as the error.
    /// Returns true when the save ran to completion, false when validation rejected the request
    /// or the save failed - so callers (e.g. telemetry wrappers) get the outcome directly
    /// instead of reading it back from the error property.
    /// </summary>
    protected static async Task<bool> ExecuteSaveAsync(
        Func<string?> validate,
        Action<string?> setError,
        Action<bool> setSaving,
        Func<Task> save,
        Action? notifyCommandsChanged = null)
    {
        var validationMessage = validate();
        if (!string.IsNullOrEmpty(validationMessage))
        {
            setError(validationMessage);
            return false;
        }

        setSaving(true);
        notifyCommandsChanged?.Invoke();
        setError(null);

        try
        {
            await save();
            return true;
        }
        catch (Exception ex)
        {
            setError(ex.Message);
            return false;
        }
        finally
        {
            setSaving(false);
            notifyCommandsChanged?.Invoke();
        }
    }
}


