using System.Diagnostics;
using System.Reflection;

ApplicationConfiguration.Initialize();
try
{
    // Install beside the setup executable. This works without administrator rights or protected-profile access.
    var target = Path.Combine(AppContext.BaseDirectory, "Teldrive Desktop");
    Directory.CreateDirectory(target);
    // Close only this wrapper when updating. The external teldrive.exe is never touched.
    var desktopExe = Path.Combine(target, "TeldriveDesktop.exe");
    foreach (var process in Process.GetProcessesByName("TeldriveDesktop"))
    {
        try
        {
            if (string.Equals(process.MainModule?.FileName, desktopExe, StringComparison.OrdinalIgnoreCase))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
        }
        catch { /* A process that cannot be inspected is not ours to stop. */ }
    }
    var asm = Assembly.GetExecutingAssembly();
    foreach (var name in asm.GetManifestResourceNames().Where(n => n.StartsWith("Payload/")))
    {
        var relative = name["Payload/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var file = Path.Combine(target, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await using var input = asm.GetManifestResourceStream(name)!;
        await using var output = File.Create(file);
        await input.CopyToAsync(output);
    }
    try
    {
        var shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Teldrive Desktop.lnk");
        var script = "$w=New-Object -ComObject WScript.Shell;" +
                     "$s=$w.CreateShortcut('" + shortcut.Replace("'", "''") + "');" +
                     "$s.TargetPath='" + desktopExe.Replace("'", "''") + "';" +
                     "$s.WorkingDirectory='" + target.Replace("'", "''") + "';" +
                     "$s.Description='Teldrive Desktop';$s.Save()";
        using var powershell = Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"") { UseShellExecute = false, CreateNoWindow = true });
        powershell?.WaitForExit(10_000);
    }
    catch { /* The app remains usable even if a locked-down profile blocks shortcut creation. */ }
    Process.Start(new ProcessStartInfo(desktopExe) { WorkingDirectory = target, UseShellExecute = true });
    MessageBox.Show("Teldrive Desktop was installed and started.", "Teldrive Desktop", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
catch (Exception ex)
{
    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "install-error.txt"), ex.ToString());
    MessageBox.Show($"Installation failed: {ex.Message}\n\nDetails were written to install-error.txt.", "Teldrive Desktop Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
