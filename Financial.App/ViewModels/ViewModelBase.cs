using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Financial.Presentation.App.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

    /// <summary>
    /// Runs the request-id-guarded refresh sequence shared by every page-level refresh: bump the
    /// request id, set loading/clear error, run <paramref name="refresh"/> (which checks
    /// <c>isCurrent</c> itself once its own fetch completes, since a multi-stage fetch may need to
    /// check more than once or partway through), and on the way out only the request that is still
    /// current is allowed to set Error or clear IsLoading - an older overlapping call must not
    /// clobber a newer one's state.
    /// </summary>
    protected static async Task ExecuteRefreshAsync(
        Func<int> beginRequest,
        Func<int, bool> isCurrentRequest,
        Action<bool> setLoading,
        Action<string?> setError,
        Func<Func<bool>, Task> refresh,
        Action<Exception>? logError = null)
    {
        var requestId = beginRequest();
        setLoading(true);
        setError(null);

        try
        {
            await refresh(() => isCurrentRequest(requestId));
        }
        catch (Exception ex)
        {
            logError?.Invoke(ex);
            if (isCurrentRequest(requestId))
            {
                setError(ex.Message);
            }
        }
        finally
        {
            if (isCurrentRequest(requestId))
            {
                setLoading(false);
            }
        }
    }
}


