using System;
using Avalonia.Threading;
using TelePick.Desktop.Views;

namespace TelePick.Desktop.Services
{
    public static class NotificationService
    {
        public static void ShowSuccess(string title, string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var notification = new NotificationWindow(title, message);
                notification.Show();
            });
        }
    }
}
