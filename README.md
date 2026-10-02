<div align="center">

# ☁️ Teldrive Desktop

### High-Performance Windows Desktop Client for Teldrive & Telegram Cloud

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=flat-square&logo=windows)](https://microsoft.com)
[![Tauri](https://img.shields.io/badge/Tauri-v2%20(Rust)-24C8DB?style=flat-square&logo=tauri)](https://tauri.app/)
[![React](https://img.shields.io/badge/React-18%20%2B%20Vite-61DAFB?style=flat-square&logo=react)](https://react.dev/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](LICENSE)
[![Author](https://img.shields.io/badge/Author-Pouya%20Sadeghpour-181717?style=flat-square&logo=github)](https://github.com/pouya-wp)

<p align="center">
  <b>Teldrive Desktop</b> is a Windows-native desktop client for the official <b>Teldrive</b> engine. It securely wraps and orchestrates the local Teldrive binary with automatic background lifecycle management, system tray integration, autostart, and crash-resilient process supervision.
</p>

</div>

---

## ✨ Features

- 🔄 **Autonomous Process Supervision** — Discovers `%USERPROFILE%\.installer\bin\teldrive.exe` or PATH, starts it hidden, and monitors HTTP readiness.
- 🛡️ **Non-Invasive Architecture** — Attaches safely to existing local Teldrive instances without modifying `config.toml` or killing external processes.
- 📥 **System Tray & Autostart** — Minimizes to tray, supports Windows boot startup, and quick tray actions ("Open Web UI", "Check Status").
- 📊 **Bounded Exponential Retries** — Smart 2/5/10/30-second backoff with redacted process logs for rapid troubleshooting.
- 📦 **Zero-Config Per-User Installer** — Clean NSIS packaging that leaves configuration intact on uninstall.

---

## 🏗️ Architecture

```
Teldrive Desktop Architecture
├── Tauri 2 Rust Core (Process orchestration, health check, tray menu)
│    ├── Candidate Discovery (%USERPROFILE%\.installer\bin, PATH, Custom)
│    ├── Process Supervisor (Hidden exec, readiness polling, retry backoff)
│    └── Redacted Log Streamer
└── Modern Web UI (Vite + React + TypeScript)
     ├── Embedded Dashboard & File Explorer
     └── Realtime Server Status & Bandwidth Monitoring
```

| Component | Responsibility | Technology |
|:---|:---|:---|
| `src-tauri/` | Native process lifecycle, system tray, window management | Rust (Tauri 2) |
| `src/` | Lightweight embedded frontend controller | React, TypeScript, Vite |
| `installer/` | NSIS packaging and install verification scripts | NSIS, PowerShell |

---

## 🚀 Quick Start

### Prerequisites
- Node.js 20+
- Rust 1.77+ with MSVC toolchain
- Windows 10/11

### Development

```powershell
# Clone the repository
git clone https://github.com/pouya-wp/teldrive-desktop.git
cd teldrive-desktop

# Install frontend dependencies
npm install

# Run Tauri development build
npm run tauri dev
```

### Production Installer Build

```powershell
npm run tauri build
```
The NSIS installer will be emitted under `src-tauri\target\release\bundle\nsis\`.

---

## 🔒 Security & Boundaries

Teldrive Desktop strictly adheres to safe engineering principles:
- Never inspects or transmits credentials or tokens outside localhost.
- Never downloads unofficial binaries or alters user files.
- All communications are strictly bound to `127.0.0.1:8080`.

---

## 📄 License

Distributed under the MIT License.

---

<div align="center">
  <sub>Crafted with precision & passion by <b><a href="https://github.com/pouya-wp">Pouya Sadeghpour</a></b></sub>
</div>
