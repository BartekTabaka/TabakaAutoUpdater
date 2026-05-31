# TabakaAutoUpdater

A simple Windows application for checking and installing software updates using **winget** (Windows Package Manager).

---

## Features

- **Automatic update check** on startup — lists all outdated packages
- **One-click update all** — runs `winget upgrade --all` in the background with live output
- **Cancel** ongoing update at any time
- **Debug / Raw mode** — shows raw winget output for diagnostics
- **Diagnostic logging** — saves detailed logs to `logs/` next to the `.exe`
- Fullscreen UI, works machine-wide (`--scope machine`)

## Requirements

- Windows 10 / 11
- [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/) (Windows Package Manager) — pre-installed on Windows 11, available via Microsoft Store on Windows 10
- .NET 6 or newer

## Installation

1. Download the latest release from [Releases](../../releases)
2. Extract the `.zip` anywhere you want
3. Run `TabakaAutoUpdater.exe`

> **Note:** The app uses `--scope machine` which checks updates for all users system-wide. No administrator privileges are required to check for updates, but installing some packages may trigger a UAC prompt.

## Usage

| Button | Description |
|--------|-------------|
| **Sprawdź** | Manually re-check for available updates |
| **Aktualizuj** | Install all pending updates |
| **Anuluj** | Cancel an ongoing update |
| **Raw** | Show raw winget output + diagnostic report |

Diagnostic logs are saved to `logs/winget_diag_<timestamp>.txt` next to the executable.

## Known Limitations

- **Microsoft Office / Microsoft 365** — managed by Click-to-Run, not visible to winget
- **Windows Update** — outside winget's scope entirely; use Windows Settings or WSUS
- Packages with unrecognized version numbers are not shown by default (winget limitation)

## How It Works

The app calls `winget upgrade --scope machine` as a subprocess, captures its output line by line, and parses the fixed-width table to extract package names. A diagnostic mode is built in to help troubleshoot parsing issues across different system configurations.

## License

MIT
