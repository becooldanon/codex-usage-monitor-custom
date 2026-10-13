# Changelog

## v1.3 — 2026-10-13

First Windows x64 GitHub Release.

### Features
- Single notification-area icon showing the lower remaining Codex rate-limit percentage
- Five-hour and seven-day allowance indicators
- Light and Dark themes
- Configurable polling intervals and low-allowance notifications (20%, 10%, 5%)
- Floating dashboard, auto-hide, global keyboard shortcut and Windows startup option
- History charts and CSV export

### Fixes
- Adjusted Codex app-server invocation for the npm-installed CLI
- Kept the last successful allowance values when a refresh fails
- Removed the experimental Transparent theme
- Fixed the remaining theme references identified during the Windows build

### Notes
- The monitor must run within the Windows user profile where the Codex CLI is authenticated.
- Values are percentages of remaining rate-limit allowance, not token balances.
- The ZIP is built and published automatically by GitHub Actions.
