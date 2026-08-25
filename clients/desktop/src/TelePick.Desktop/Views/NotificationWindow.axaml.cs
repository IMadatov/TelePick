using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;

namespace TelePick.Desktop.Views
{
    public partial class NotificationWindow : Window
    {
        public NotificationWindow()
        {
            InitializeComponent();
        }

        public NotificationWindow(string title, string message) : this()
        {
            var titleBlock = this.FindControl<TextBlock>("TitleTextBlock");
            if (titleBlock != null) titleBlock.Text = title;

            var msgBlock = this.FindControl<TextBlock>("MessageTextBlock");
            if (msgBlock != null) msgBlock.Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDismissClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Position at bottom right
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                var x = workArea.Right - Bounds.Width - 32; // 32px padding from right
                var y = workArea.Bottom - Bounds.Height - 32; // 32px padding from bottom
                Position = new PixelPoint((int)x, (int)y);
            }

            // Auto-close after 4 seconds
            DispatcherTimer.RunOnce(() =>
            {
                if (IsVisible)
                {
                    Close();
                }
            }, TimeSpan.FromSeconds(4));
        }
    }
}
