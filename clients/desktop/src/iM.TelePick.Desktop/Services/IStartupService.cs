namespace iM.TelePick.Desktop.Services;

public interface IStartupService
{
    bool IsEnabled();
    void Enable();
    void Disable();
}
