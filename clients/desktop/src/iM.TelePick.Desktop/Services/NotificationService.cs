using System;
using Avalonia.Threading;
using iM.TelePick.Desktop.Views;

namespace iM.TelePick.Desktop.Services
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
