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
        }

        private void OnViewKeyDown(object? sender, KeyEventArgs e)
        {
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            var modifier = isMac ? KeyModifiers.Meta : KeyModifiers.Control;

            if (e.Key == Key.K && e.KeyModifiers.HasFlag(modifier))
            {
                var searchBox = this.FindControl<TextBox>("SearchTextBox");
                searchBox?.Focus();
                e.Handled = true;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
