using System.Collections.Generic;
using System.Windows;

namespace Financial.Investment.Infrastructure.Tools.ImportGoogleSpreadSheets;

public partial class ImportIssuesWindow : Window
{
    public string IssuesText { get; }
    public string IssueCountText { get; }

    public ImportIssuesWindow(IReadOnlyCollection<string> issues)
    {
        InitializeComponent();
        IssuesText = string.Join(System.Environment.NewLine, issues);
        IssueCountText = issues.Count == 1 ? "1 issue found during import:" : $"{issues.Count} issues found during import:";
        DataContext = this;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
