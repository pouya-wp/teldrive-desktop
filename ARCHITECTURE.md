# Architecture

`Teldrive Desktop` has two independent processes: this Tauri application and the official `teldrive.exe`.

```text
React / WebView2 shell
        │ Tauri commands
Rust BackendManager ── discovery ──> official teldrive.exe run
        │ health polling                         │ stdout/stderr (redacted)
        └──────────── embeds http://127.0.0.1:8080 ┘
```

## Lifecycle

1. The manager checks the local HTTP endpoint first. A healthy endpoint is marked `Running — External`; no process is owned or stopped.
2. Otherwise it validates the existing config and official executable, starts `run` with `CREATE_NO_WINDOW`, and polls HTTP every 500 ms for at most 30 seconds.
3. The manager owns only the child it starts. It detects that child exiting and retries at 2, 5, 10, then 30 seconds; further failures stop recovery.
4. Closing the window hides it. Tray Quit stops only the owned child, then exits.

## Persistent data

- Existing Teldrive config: `%USERPROFILE%\.teldrive\config.toml` (read only for existence).
- Wrapper preferences: user configuration directory, `Teldrive Desktop/settings.json`.

The discovery and settings modules intentionally make a future official sidecar source easy to add ahead of installed-binary discovery.
