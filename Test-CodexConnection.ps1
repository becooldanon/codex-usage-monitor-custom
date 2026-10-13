# Diagnostic only. Do not log authentication files, tokens or app-server payloads.
$ErrorActionPreference = "Continue"
Write-Host "Codex CLI command" -ForegroundColor Cyan
Get-Command codex.cmd -ErrorAction SilentlyContinue | Select-Object Name, Source
Write-Host "Codex CLI version" -ForegroundColor Cyan
& codex.cmd --version
Write-Host "Codex CLI authentication status" -ForegroundColor Cyan
& codex.cmd login status
Write-Host "Codex CLI exit status: $LASTEXITCODE"
Write-Host "CODEX_HOME: $(if ($env:CODEX_HOME) { 'Configured' } else { 'Default' })"
Write-Host "Monitor log" -ForegroundColor Cyan
$log = Join-Path $env:LOCALAPPDATA 'CodexUsageMonitorCustom\diagnostics.log'
if (Test-Path $log) { Get-Content $log -Tail 10 } else { Write-Host 'No diagnostic entries yet. Run Refresh now.' }
Write-Host "If Codex is signed in but the reader still fails, share the exact error message only. Do not share auth.json or tokens."