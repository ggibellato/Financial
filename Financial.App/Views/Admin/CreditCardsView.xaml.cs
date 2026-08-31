using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Admin;

namespace Financial.Presentation.App.Views.Admin;

public partial class CreditCardsView : UserControl
{
    public CreditCardsView(CreditCardsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
