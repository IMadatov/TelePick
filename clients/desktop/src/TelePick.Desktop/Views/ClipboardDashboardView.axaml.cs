using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;

namespace TelePick.Desktop.Views
{
    public partial class ClipboardDashboardView : UserControl
    {
        public ClipboardDashboardView()
        {
            InitializeComponent();
            this.KeyDown += OnViewKeyDown;

            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == Control.IsVisibleProperty && this.IsVisible)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var searchBox = this.FindControl<TextBox>("SearchTextBox");
                        searchBox?.Focus();
                    });
                }
            };
        }

        private void OnViewKeyDown(object? sender, KeyEventArgs e)
        {
            if (this.DataContext is ViewModels.MainWindowViewModel vm && !string.IsNullOrEmpty(vm.LocalSearchFocusHotkey))
            {
                try
                {
                    var gesture = KeyGesture.Parse(vm.LocalSearchFocusHotkey);
                    if (gesture.Matches(e))
                    {
                        var searchBox = this.FindControl<TextBox>("SearchTextBox");
                        searchBox?.Focus();
                        e.Handled = true;
                    }
                }
                catch { }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
