# Codex Usage Monitor Custom

A Windows 11 system tray utility for monitoring **Codex 5-hour and 7-day usage limits**, built with C# / .NET 10.

> **v1.3 release candidate.** Usage reading has been confirmed on Windows 11 when the monitor runs in the same Windows user session as the authenticated Codex CLI. The final source changes still need a clean Windows build before publishing a binary release.

## Features
- Two selectable themes: **Light** and **Dark**
- One tray icon showing the **lower remaining percentage**; tooltip reports both limits
- Adjustable polling interval (1, 2, 5, 10, 15 or 30 minutes)
- Notifications at configurable **20%, 10% and 5%** remaining limits
- Light/Dark dashboard; optional always-on-top, draggable floating widget
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
- Network access to download the upstream usage models on first build

## Troubleshooting Codex CLI on Windows

The monitor calls the npm-installed Codex CLI using `node.exe` and its `bin/codex.js` entry point, rather than wrapping `codex` through `cmd.exe` (which may prematurely close redirected standard input). The CLI must be installed in the current user's standard npm global directory.

```powershell
codex.cmd --version
codex.cmd login status
Test-Path "$env:APPDATA\npm\node_modules\@openai\codex\bin\codex.js"
```

If a refresh fails, the monitor retains its last successful values, which are **stale** until another successful refresh. Review `%LOCALAPPDATA%\CodexUsageMonitorCustom\diagnostics.log`.

**Important:** Run the monitor in the same Windows user session where the Codex CLI is signed in. Running from a separate administrator account will use a different user profile and may not find the authenticated CLI.

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

Inspired by [Tooblippe/codex-usage](https://github.com/Tooblippe/codex-usage), licensed under MIT. The build script downloads `UsageModels.cs` and the upstream `LICENSE` (a patched `CodexRateLimitReader.cs` is maintained in this repository) from that project's `main` branch. Preserve upstream notices when distributing modified copies or binaries. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

MIT licensed for this project's original code. Not affiliated with or endorsed by OpenAI or the upstream author.

## Validation status

The final v1.3 source cleanup has **not been independently compiled in this environment**. The monitor works on the user's Windows installation when launched from the authenticated Windows session. No prebuilt executable or GitHub Release has been published yet.
