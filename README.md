# Codex Usage Monitor Custom

A Windows 11 system tray utility for monitoring **Codex 5-hour and 7-day usage limits**, built with C# / .NET 10.

> **Development build (v1.1).** The v1.0.1 build was tested in Windows 11. All v1.1 changes still require full Windows compilation and testing before a binary release.

## Features
- Three selectable themes: **Light**, **Dark**, and **Transparent** (75% maximum window opacity)
- One tray icon showing the **lower remaining percentage**; tooltip reports both limits
- Adjustable polling interval (1, 2, 5, 10, 15 or 30 minutes)
- Notifications at configurable **20%, 10% and 5%** remaining limits
- Dark dashboard; optional always-on-top, draggable floating widget
- Global **Ctrl+Alt+C** show/hide shortcut (if available)
- Auto-hide dashboard after 5 seconds when not in permanent floating mode
- Configurable opacity (60–100%)
- History views for 1h, 6h, 24h, 7d and 30d; CSV export
- Optional start with Windows
- Separate settings/history and installation from the original Codex Usage Tray

## Requirements
- Windows 11 x64
- .NET 10 SDK and **Windows Desktop Runtime 10**
- Authenticated `codex.cmd` CLI (ChatGPT sign-in)
- Network access to download the upstream reader/models on first build

## Troubleshooting Codex CLI on Windows

The monitor calls the npm-installed Codex CLI using `node.exe` and its `bin/codex.js` entry point, rather than wrapping `codex` through `cmd.exe` (which may prematurely close redirected standard input). The CLI must be installed in the current user's standard npm global directory.

```powershell
codex.cmd --version
codex.cmd login status
Test-Path "$env:APPDATA\npm\node_modules\@openai\codex\bin\codex.js"
```

If a refresh fails, the monitor retains its last successful values, which are **stale** until another successful refresh. Review `%LOCALAPPDATA%\CodexUsageMonitorCustom\diagnostics.log`.

The Transparent mode uses WinForms color-key transparency; anti-aliased glyph edges may still show faint halos. A per-pixel layered window is required for fully clean transparency.

## Build and install

```powershell
.\Build-And-Install.ps1
```

To compile without installing:

```powershell
.\Build-And-Install.ps1 -BuildOnly
```

If Windows blocks unsigned PowerShell scripts, follow your organisation's security policy; do not bypass managed controls.

The application installs into `%LOCALAPPDATA%\Programs\CodexUsageMonitorCustom` and saves user data in `%LOCALAPPDATA%\CodexUsageMonitorCustom`.

## About usage history

History is recorded when this application polls Codex; **earlier sessions cannot be backfilled** automatically. Usage charts show rate-limit *remaining percentages*, not exact token counts. The local Codex app-server interface is experimental and can change.

## Attribution and licensing

Inspired by [Tooblippe/codex-usage](https://github.com/Tooblippe/codex-usage), licensed under MIT. The build script downloads `CodexRateLimitReader.cs`, `UsageModels.cs` and the upstream `LICENSE` from that project's `main` branch. Preserve upstream notices when distributing modified copies or binaries. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

MIT licensed for this project's original code. Not affiliated with or endorsed by OpenAI or the upstream author.

## Validation status

The v1.1 source has **not yet been compiled or smoke-tested on Windows**. Treat it as development source until validated. No prebuilt executable is published.
