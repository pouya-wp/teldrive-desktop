using Microsoft.Win32;

namespace TeldriveDesktop.Services;

public static class AutoStart
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TeldriveDesktop";
    public static void Set(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath)!;
        if (enabled) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" --autostart");
        else key.DeleteValue(ValueName, false);
    }
}
