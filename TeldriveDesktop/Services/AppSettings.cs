using System.Text.Json;

namespace TeldriveDesktop.Services;

public sealed class AppSettings
{
    public string? CustomExecutable { get; set; }
    public bool StartWithWindows { get; set; } = true;
    public bool LaunchBackendAutomatically { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Teldrive Desktop", "settings.json");
    public static AppSettings Load()
    {
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
