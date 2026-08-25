using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TelePick.Desktop.Services;
using TelePick.Desktop.ViewModels;
using TelePick.Desktop.Views;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TelePick.Desktop;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    private IServiceProvider? _serviceProvider;
    private MainWindow? _mainWindow;

    public ICommand ShowWindowCommand { get; }
    public ICommand QuitCommand { get; }

    public App()
    {
        ShowWindowCommand = new SimpleCommand(ShowMainWindow);
        QuitCommand = new SimpleCommand(QuitApplication);
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Set DataContext for TrayIcon bindings
        DataContext = this;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        var hotkeyService = _serviceProvider.GetRequiredService<IGlobalHotkeyService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var monitorService = _serviceProvider.GetRequiredService<IClipboardMonitorService>();
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        hotkeyService.Start();
        
        // Wait for settings to load before registering hotkeys
        Task.Run(async () => 
        {
            var settings = await settingsService.LoadSettingsAsync();
            
            // Quick Paste
            hotkeyService.RegisterHotkey("QuickPaste", settings.ClipboardPopupHotkey, () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var window = new ClipboardPopupWindow 
                    { 
                        DataContext = viewModel
                    };
                    
                    window.Position = new Avalonia.PixelPoint(hotkeyService.LastMouseX, hotkeyService.LastMouseY);
                    window.Show();
                    window.Activate();
                });
            });

            // Send to Telegram
            hotkeyService.RegisterHotkey("SendToTelegram", settings.SendToTelegramHotkey, () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (viewModel.SendToTelegramCommand.CanExecute(null))
                    {
                        viewModel.SendToTelegramCommand.Execute(null);
                    }
                });
            });

            // Global Search
            hotkeyService.RegisterHotkey("GlobalSearch", settings.GlobalSearchHotkey, () =>
            {
                // Placeholder
            });

            // Clear History
            hotkeyService.RegisterHotkey("ClearHistory", settings.ClearHistoryHotkey, () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    monitorService.History.Clear();
                });
            });

            // Pause Monitoring
            hotkeyService.RegisterHotkey("PauseMonitoring", settings.PauseMonitoringHotkey, () =>
            {
                monitorService.IsPaused = !monitorService.IsPaused;
            });
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Keep running when main window is closed (tray mode)
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
            };
            desktop.MainWindow = _mainWindow;

            // Hide to tray instead of quitting when close button is clicked
            _mainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                _mainWindow.Hide();
            };

            if (_mainWindow.Clipboard != null)
            {
                monitorService.StartMonitoring(_mainWindow.Clipboard);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.WindowState = WindowState.Normal;
        }
    }

    private void QuitApplication()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Detach closing handler so we can actually close
            if (_mainWindow != null)
            {
                _mainWindow.Closing -= null!;
                _mainWindow.Close();
            }

            var monitorService = _serviceProvider?.GetService<IClipboardMonitorService>();
            monitorService?.StopMonitoring();

            desktop.Shutdown();
        }
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

/// <summary>
/// Simple ICommand implementation for tray icon commands.
/// </summary>
internal class SimpleCommand : ICommand
{
    private readonly Action _execute;

    public SimpleCommand(Action execute) => _execute = execute;

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}