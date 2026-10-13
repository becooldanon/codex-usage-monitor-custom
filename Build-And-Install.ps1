param(
    [switch]$BuildOnly,
    [switch]$Standalone
)

$ErrorActionPreference = 'Stop'
$projectRoot = Join-Path $PSScriptRoot 'Source'
$reader = Join-Path $projectRoot 'CodexRateLimitReader.cs'
$models = Join-Path $projectRoot 'UsageModels.cs'
$upstream = 'https://raw.githubusercontent.com/Tooblippe/codex-usage/main'

# Download only the original Codex protocol reader and data models.
# The customized application uses separate settings, history and startup keys.
foreach ($file in @('CodexRateLimitReader.cs', 'UsageModels.cs')) {
    $destination = Join-Path $projectRoot $file
    if (-not (Test-Path $destination)) {
        Write-Host "Downloading upstream dependency: $file"
        Invoke-WebRequest -Uri "$upstream/$file" -OutFile $destination
    }
}

# Retain the upstream license alongside its imported reader.
$license = Join-Path $PSScriptRoot 'UPSTREAM-LICENSE.txt'
if (-not (Test-Path $license)) { Invoke-WebRequest -Uri "$upstream/LICENSE" -OutFile $license }

# Default to framework-dependent, multi-file deployment to avoid downloading runtime packs.
# Use -Standalone only when NuGet.org is reachable and a fully standalone package is required.
if (-not $Standalone) {
    $desktopRuntime = dotnet --list-runtimes | Where-Object { $_ -match '^Microsoft.WindowsDesktop.App 10\.' }
    if (-not $desktopRuntime) {
        throw 'Microsoft.WindowsDesktop.App 10 runtime is missing. Install the .NET 10 Desktop Runtime, or build using -Standalone with NuGet access.'
    }
}

Write-Host 'Publishing Codex Usage Monitor Custom...'
$publishArgs = @(
    'publish', (Join-Path $projectRoot 'CodexUsageMonitorCustom.csproj'),
    '-c', 'Release', '-r', 'win-x64',
    '--self-contained', $(if ($Standalone) { 'true' } else { 'false' }),
    '-p:PublishSingleFile=false',
    '-p:PublishTrimmed=false',
    '-p:PublishReadyToRun=false',
    '-o', (Join-Path $PSScriptRoot 'publish')
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'Publish failed. Check your NuGet sources with dotnet nuget list source.' }

if (-not $BuildOnly) {
    $target = Join-Path $env:LOCALAPPDATA 'Programs\CodexUsageMonitorCustom'
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $targetExe = Join-Path $target 'CodexUsageMonitorCustom.exe'
    if (Get-Process CodexUsageMonitorCustom -ErrorAction SilentlyContinue) {
        throw 'Close the customized monitor using its Exit menu before installing a new build.'
    }
    Copy-Item (Join-Path $PSScriptRoot 'publish\*') $target -Recurse -Force
    Write-Host "Installed: $targetExe"
    Start-Process $targetExe
}