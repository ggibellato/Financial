namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Binding source for <see cref="Views.Admin.AdminEntityPlaceholderView"/>, the shared "coming soon"
/// placeholder every Admin entity leaf uses until its own CRUD screen (F02-F11) replaces it.
/// </summary>
public class AdminEntityPlaceholderViewModel(string entityLabel) : ViewModelBase
{
    public string EntityLabel { get; } = entityLabel;
}
