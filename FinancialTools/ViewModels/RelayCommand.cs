using System;
using System.Windows.Input;

namespace SharesDividendCheck.ViewModels;

/// <summary>
/// Simple ICommand implementation for use in ViewModels
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Creates a new RelayCommand
    /// </summary>
    /// <param name="execute">Action to execute when the command is invoked</param>
    /// <param name="canExecute">Optional predicate to determine if command can execute</param>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Creates a new parameterless RelayCommand
    /// </summary>
    /// <param name="execute">Action to execute when the command is invoked</param>
    /// <param name="canExecute">Optional function to determine if command can execute</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
    {
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    /// <summary>
    /// Manually raise CanExecuteChanged to re-evaluate command state
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
