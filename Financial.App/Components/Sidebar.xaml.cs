using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Financial.Presentation.App.ViewModels;

namespace Financial.Presentation.App.Components
{
    /// <summary>
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : UserControl
    {
        private const int CloseDelayMilliseconds = 250;

        private readonly DispatcherTimer _closeTimer;
        private Popup? _openPopup;
        private FrameworkElement? _openTrigger;
        private bool _suppressFocusOpen;

        public Sidebar()
        {
            InitializeComponent();

            _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(CloseDelayMilliseconds) };
            _closeTimer.Tick += (_, _) =>
            {
                _closeTimer.Stop();
                ClosePopupNow();
            };
        }

        private void CategoryHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenFlyout((FrameworkElement)sender);
        }

        private void CategoryHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            ScheduleClose();
        }

        private void CategoryHeader_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            OpenFlyout((FrameworkElement)sender);
        }

        private void CategoryHeader_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!IsWithinCurrentFlyout(e.NewFocus as DependencyObject))
            {
                ClosePopupNow();
            }
        }

        private void CategoryHeader_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _openTrigger == sender)
            {
                ClosePopupNow(refocus: true);
                e.Handled = true;
            }
        }

        private void FlyoutPopup_MouseEnter(object sender, MouseEventArgs e)
        {
            _closeTimer.Stop();
        }

        private void FlyoutPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            ScheduleClose();
        }

        private void FlyoutPopup_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClosePopupNow(refocus: true);
                e.Handled = true;
            }
        }

        private void FlyoutChild_Click(object sender, RoutedEventArgs e)
        {
            ClosePopupNow();
        }

        private void OpenFlyout(FrameworkElement trigger)
        {
            if (_suppressFocusOpen)
            {
                _suppressFocusOpen = false;
                return;
            }

            if (DataContext is not MainShellViewModel { IsCollapsed: true })
            {
                return;
            }

            _closeTimer.Stop();

            if (trigger.Parent is not Panel root)
            {
                return;
            }

            var popup = root.Children.OfType<Popup>().SingleOrDefault();
            if (popup == null)
            {
                return;
            }

            if (_openPopup != null && _openPopup != popup)
            {
                _openPopup.IsOpen = false;
            }

            popup.IsOpen = true;
            _openPopup = popup;
            _openTrigger = trigger;
        }

        private void ScheduleClose()
        {
            _closeTimer.Stop();
            _closeTimer.Start();
        }

        private void ClosePopupNow(bool refocus = false)
        {
            _closeTimer.Stop();

            var trigger = _openTrigger;

            if (_openPopup != null)
            {
                _openPopup.IsOpen = false;
                _openPopup = null;
            }

            _openTrigger = null;

            if (refocus && trigger != null)
            {
                _suppressFocusOpen = true;
                trigger.Focus();
            }
        }

        private bool IsWithinCurrentFlyout(DependencyObject? focusTarget)
        {
            if (focusTarget == null)
            {
                return false;
            }

            if (_openTrigger != null && Equals(focusTarget, _openTrigger))
            {
                return true;
            }

            return _openPopup?.Child is DependencyObject popupContent && IsDescendant(popupContent, focusTarget);
        }

        private static bool IsDescendant(DependencyObject ancestor, DependencyObject node)
        {
            DependencyObject? current = node;
            while (current != null)
            {
                if (Equals(current, ancestor))
                {
                    return true;
                }

                current = current is Visual
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }
    }
}
