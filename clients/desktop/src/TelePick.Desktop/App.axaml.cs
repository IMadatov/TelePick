using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TelePick.Desktop.Services;
using TelePick.Desktop.ViewModels;
using TelePick.Desktop.Views;
using System;

namespace TelePick.Desktop;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
            };
            desktop.MainWindow = mainWindow;
            var monitorService = _serviceProvider.GetRequiredService<IClipboardMonitorService>();
            if (mainWindow.Clipboard != null)
            {
                monitorService.StartMonitoring(mainWindow.Clipboard);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        
        // Register Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ITelegramService, TelegramService>();
        services.AddSingleton<IClipboardMonitorService, ClipboardMonitorService>();
        services.AddSingleton<IGlobalHotkeyService, SharpHookGlobalHotkeyService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
    }
}