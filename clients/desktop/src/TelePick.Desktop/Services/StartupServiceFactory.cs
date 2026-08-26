using System.Runtime.InteropServices;

namespace TelePick.Desktop.Services;

public static class StartupServiceFactory
{
    public static IStartupService Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxStartupService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // WindowsStartupService to be created in Task 3
            return new DummyStartupService();
        }
        
        return new DummyStartupService();
    }
}

public class DummyStartupService : IStartupService
{
    public bool IsEnabled() => false;
    public void Enable() { }
    public void Disable() { }
}
