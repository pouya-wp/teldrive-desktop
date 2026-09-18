# Teldrive Desktop (.NET 10)

Lightweight native Windows wrapper around the already installed official Teldrive binary. It does not alter, fork, or rebuild Teldrive.

## Build

```powershell
dotnet restore
dotnet publish -c Release -r win-x64 -o .\publish
```

The application needs .NET 10 Desktop Runtime and Microsoft Edge WebView2 Runtime. It uses no Visual Studio IDE, Rust, Node.js, Electron, or Tauri.

## Lifecycle

- Finds `%USERPROFILE%\.installer\bin\teldrive.exe` or a selected official executable.
- Verifies `teldrive version`, checks that `%USERPROFILE%\.teldrive\config.toml` exists, and starts `teldrive.exe run` with no console window.
- Polls local HTTP readiness rather than delaying arbitrarily.
- If Teldrive was already running, attaches without owning or killing it.
- Restarts app-owned crashes after 2, 5, 10, and 30 seconds only.
- Closing hides to tray. Quit stops only the child this app started.
