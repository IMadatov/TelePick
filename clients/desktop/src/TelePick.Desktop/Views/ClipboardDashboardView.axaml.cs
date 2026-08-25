using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TelePick.Desktop.Views
{
    public partial class ClipboardDashboardView : UserControl
    {
        public ClipboardDashboardView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
