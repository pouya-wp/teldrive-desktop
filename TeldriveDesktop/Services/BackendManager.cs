using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;

namespace TeldriveDesktop.Services;

public enum BackendState { Stopped, Starting, Running, Unhealthy, Restarting, Failed }
public sealed record BackendStatus(BackendState State, string Detail, bool External, string? Executable, string? Version, string ConfigPath);
public sealed record LogLine(DateTimeOffset At, string Stream, string Text);

public sealed class BackendManager : IDisposable
{
    public const string ServerUrl = "http://127.0.0.1:8080";
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly AppSettings _settings;
    private readonly List<LogLine> _logs = [];
    private readonly object _gate = new();
    private Process? _process;
    private int _retries;
    public event EventHandler<BackendStatus>? StatusChanged;
    public BackendStatus Status { get; private set; }
    public IReadOnlyList<LogLine> Logs { get { lock (_gate) return _logs.ToArray(); } }
    public string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".teldrive", "config.toml");

    public BackendManager(AppSettings settings)
    {
        _settings = settings;
        Status = new(BackendState.Stopped, "Ready to start", false, null, null, ConfigPath);
    }

    public async Task StartAsync()
    {
        if (await IsHealthyAsync()) { SetStatus(BackendState.Running, "Connected to existing Teldrive", true); return; }
        if (!File.Exists(ConfigPath)) { SetStatus(BackendState.Failed, "Teldrive configuration was not found."); return; }
        if (!_settings.LaunchBackendAutomatically) { SetStatus(BackendState.Stopped, "Backend launch is disabled in settings."); return; }
        if (!CanBindPort()) { SetStatus(BackendState.Failed, "Port 8080 is occupied by an application that is not Teldrive."); return; }
        string? executable = FindExecutable();
        if (executable is null) { SetStatus(BackendState.Failed, "Teldrive executable was not found."); return; }
        string? version = await GetVersionAsync(executable);
        if (version is null) { SetStatus(BackendState.Failed, "Teldrive version verification failed."); return; }
        lock (_gate) if (_process is not null && !_process.HasExited) return;
        SetStatus(BackendState.Starting, "Starting Teldrive…", false, executable, version);
        try
        {
            var process = new Process { StartInfo = new ProcessStartInfo(executable, "run") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true } };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) AddLog("stdout", e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) AddLog("stderr", e.Data); };
            process.EnableRaisingEvents = true;
            process.Exited += async (_, _) => await RecoverAsync();
            process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
            lock (_gate) _process = process;
            AddLog("system", "Started app-managed Teldrive process.");
            for (var i = 0; i < 60; i++) { if (await IsHealthyAsync()) { _retries = 0; SetStatus(BackendState.Running, "Teldrive is ready", false, executable, version); return; } await Task.Delay(500); }
            SetStatus(BackendState.Failed, "Teldrive did not respond within 30 seconds. Check the logs.");
        }
        catch (Exception ex) { AddLog("system", ex.Message); SetStatus(BackendState.Failed, "Unable to start Teldrive."); }
    }

    public async Task RestartAsync() { Stop(); SetStatus(BackendState.Restarting, "Restarting Teldrive…"); await StartAsync(); }
    public void Stop()
    {
        Process? process; lock (_gate) { process = _process; _process = null; }
        if (process is { HasExited: false }) { process.Kill(entireProcessTree: true); AddLog("system", "Stopped app-managed Teldrive process."); }
        SetStatus(BackendState.Stopped, "Stopped");
    }
    private async Task RecoverAsync()
    {
        Process? process; lock (_gate) process = _process;
        if (process is null) return; // Explicit Stop removed ownership first.
        _retries++;
        if (_retries > 4) { SetStatus(BackendState.Failed, "Teldrive crashed repeatedly; automatic recovery stopped."); return; }
        int delay = new[] { 2, 5, 10, 30 }[_retries - 1];
        SetStatus(BackendState.Unhealthy, $"Teldrive stopped unexpectedly. Retrying in {delay} seconds…");
        await Task.Delay(TimeSpan.FromSeconds(delay));
        lock (_gate) _process = null;
        await StartAsync();
    }
    private async Task<bool> IsHealthyAsync() { try { using var response = await _http.GetAsync(ServerUrl); return (int)response.StatusCode is >= 200 and < 400; } catch { return false; } }
    private static bool CanBindPort() { try { using var listener = new TcpListener(System.Net.IPAddress.Loopback, 8080); listener.Start(); return true; } catch { return false; } }
    private string? FindExecutable()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var choices = new List<string?> { Path.Combine(home, ".installer", "bin", "teldrive.exe"), _settings.CustomExecutable, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Teldrive", "teldrive.exe") };
        try
        {
            using var where = Process.Start(new ProcessStartInfo("where.exe", "teldrive") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true });
            if (where is not null) { choices.AddRange(where.StandardOutput.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)); where.WaitForExit(); }
        }
        catch { /* PATH lookup is optional. */ }
        return choices.Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)).Select(path => path!).FirstOrDefault();
    }
    private static async Task<string?> GetVersionAsync(string executable)
    {
        try { using var p = Process.Start(new ProcessStartInfo(executable, "version") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true }); if (p is null) return null; string output = await p.StandardOutput.ReadToEndAsync(); await p.WaitForExitAsync(); return p.ExitCode == 0 ? output.Trim() : null; } catch { return null; }
    }
    private void SetStatus(BackendState state, string detail, bool external = false, string? executable = null, string? version = null) { Status = new(state, detail, external, executable ?? Status.Executable, version ?? Status.Version, ConfigPath); StatusChanged?.Invoke(this, Status); }
    private void AddLog(string stream, string text) { text = Redact(text); lock (_gate) { _logs.Add(new(DateTimeOffset.Now, stream, text)); if (_logs.Count > 500) _logs.RemoveAt(0); } }
    private static string Redact(string text) => text.Contains("password=", StringComparison.OrdinalIgnoreCase) || text.Contains("secret=", StringComparison.OrdinalIgnoreCase) || (text.Contains("://") && text.Contains('@')) ? "[Sensitive backend output redacted]" : text;
    public void Dispose() { _http.Dispose(); Stop(); }
}
