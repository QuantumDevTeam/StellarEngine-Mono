<#
.SYNOPSIS
    Installs StellarEngine on a developer machine: checks the toolchain, clones the core, builds and installs it,
    fetches the modules.

.DESCRIPTION
    The one file a machine needs to get the engine. Served from a web server or GitHub Pages:

        irm https://<host>/install.ps1 | iex

    or, with options:

        & ([scriptblock]::Create((irm https://<host>/install.ps1))) -Path D:\StellarEngine -Modules Stellar.FileSystem

    Piped into iex a script gets no parameters, so every option also reads an environment variable
    (STELLAR_INSTALL_PATH, STELLAR_INSTALL_REF, ...). Nothing is installed into the system by this script: missing
    tools are reported with the command that installs them. The installation itself is scripts/build.ps1 -Install of
    the core (registration in ~/.stellar, the 'stellar' command), undone by scripts/build.ps1 -Uninstall.

.PARAMETER Path
    Where the engine goes: the checkout of the core is the root of the installation. $STELLAR_INSTALL_PATH, or
    ~/StellarEngine.

.PARAMETER Repository
    The core. $STELLAR_INSTALL_REPOSITORY, or StellarEngine-Core of QuantumDevTeam.

.PARAMETER Ref
    The branch or tag of the core: a tag is a release. $STELLAR_INSTALL_REF, or master.

.PARAMETER Modules
    Modules to clone into modules/<Module>/, by name in the organization of the core or as full git URLs.
    $STELLAR_INSTALL_MODULES, comma separated. Later the orchestra will list them itself.

.PARAMETER Configuration
    Debug or Release. $STELLAR_INSTALL_CONFIGURATION, or Release.

.PARAMETER CheckOnly
    Only checks the toolchain.
#>
param(
    [string]$Path = $env:STELLAR_INSTALL_PATH,
    [string]$Repository = $env:STELLAR_INSTALL_REPOSITORY,
    [string]$Ref = $env:STELLAR_INSTALL_REF,
    [string[]]$Modules = @(),
    [string]$Configuration = $env:STELLAR_INSTALL_CONFIGURATION,
    [switch]$CheckOnly
)

# Everything runs inside a function: piped into iex, an 'exit' would close the shell of the user
function Install-StellarEngine {
    param($Path, $Repository, $Ref, $Modules, $Configuration, $CheckOnly)

    $ErrorActionPreference = 'Stop'
    if (-not $Path) { $Path = Join-Path $HOME 'StellarEngine' }
    if (-not $Repository) { $Repository = 'https://github.com/QuantumDevTeam/StellarEngine-Core.git' }
    if (-not $Ref) { $Ref = 'master' }
    if (-not $Configuration) { $Configuration = 'Release' }
    if ($Modules.Count -eq 0 -and $env:STELLAR_INSTALL_MODULES) {
        $Modules = $env:STELLAR_INSTALL_MODULES -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    }

    function Write-Title([string]$Text) { Write-Host "`n== $Text" -ForegroundColor Cyan }
    function Write-Check([string]$Name, [string]$State, [string]$Detail, [string]$Color) {
        Write-Host ('  {0,-26}' -f $Name) -NoNewline
        Write-Host ('{0,-9}' -f $State) -ForegroundColor $Color -NoNewline
        Write-Host $Detail
    }

    Write-Host 'StellarEngine installer' -ForegroundColor Magenta

    # ---- The toolchain ----
    Write-Title 'Toolchain'
    $missing = [System.Collections.Generic.List[string]]::new()
    $onWindows = $env:OS -eq 'Windows_NT'

    if (-not $onWindows) {
        Write-Check 'Operating system' 'missing' 'Windows only for now: the native core builds with Visual Studio' Red
        return
    }
    Write-Check 'Operating system' 'ok' 'Windows' Green

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) { Write-Check 'Git' 'ok' ((& git --version) -replace '^git version ', '') Green }
    else {
        Write-Check 'Git' 'missing' '' Red
        $missing.Add('winget install --id Git.Git -e')
    }

    # global.json of the core: 9.0.300 with rollForward latestFeature, so any 9.0 SDK of feature band 3xx or later
    $sdks = @()
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $sdks = & dotnet --list-sdks | ForEach-Object { ($_ -split ' ')[0] } |
            Where-Object { $_ -match '^9\.0\.(\d+)$' -and [int]$Matches[1] -ge 300 }
    }
    if ($sdks) { Write-Check '.NET SDK 9.0.300+' 'ok' ($sdks | Select-Object -Last 1) Green }
    else {
        Write-Check '.NET SDK 9.0.300+' 'missing' '' Red
        $missing.Add('winget install --id Microsoft.DotNet.SDK.9 -e')
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $vs = $null
    $cpp = $null
    $cli = $null
    if (Test-Path $vswhere) {
        $vs = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property displayName
        $cpp = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
        $cli = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.CLI.Support -property installationPath
    }
    $vsInstall = 'winget install --id Microsoft.VisualStudio.2022.Community -e --override "--add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.Component.VC.CLI.Support --includeRecommended --passive"'
    if ($vs) { Write-Check 'Visual Studio (MSBuild)' 'ok' "$vs" Green }
    else { Write-Check 'Visual Studio (MSBuild)' 'missing' '' Red }

    if ($cpp) { Write-Check 'C++ toolchain' 'ok' 'Desktop development with C++' Green }
    else { Write-Check 'C++ toolchain' 'missing' 'the native core is C++' Red }
    if (-not $vs -or -not $cpp) { $missing.Add($vsInstall) }

    if ($cli) { Write-Check 'C++/CLI support' 'ok' 'both wrappers of the native core are built' Green }
    else { Write-Check 'C++/CLI support' 'optional' 'not found: the engine uses the P/Invoke wrapper (Managed), which works the same' Yellow }

    if ($missing.Count -gt 0) {
        Write-Host "`nInstall what is missing, open a new terminal and run the installer again:" -ForegroundColor Red
        $missing | Select-Object -Unique | ForEach-Object { Write-Host "  $_" }
        return
    }
    if ($CheckOnly) {
        Write-Host "`nEverything StellarEngine needs is here." -ForegroundColor Green
        return
    }

    # ---- The core ----
    Write-Title "The core: $Repository ($Ref)"
    $Path = [System.IO.Path]::GetFullPath($Path)
    $isCore = Test-Path (Join-Path $Path 'Shared/Build/Stellar.Versions.props')

    if ($isCore -and (Test-Path (Join-Path $Path '.git'))) {
        Write-Host "  $Path holds the core already: updating it"
        & git -C $Path fetch --tags --quiet origin
        if ($LASTEXITCODE -ne 0) { throw 'git fetch failed' }
        & git -C $Path checkout --quiet $Ref
        if ($LASTEXITCODE -ne 0) { throw "git checkout $Ref failed: commit or stash your changes in $Path first" }
        # A branch moves on; a tag stays where it is
        $onBranch = (& git -C $Path rev-parse --abbrev-ref HEAD) -ne 'HEAD'
        if ($onBranch) {
            & git -C $Path pull --ff-only --quiet
            if ($LASTEXITCODE -ne 0) { throw "git pull failed: $Path has commits of its own on $Ref" }
        }
    }
    elseif ((Test-Path $Path) -and (Get-ChildItem $Path -Force | Select-Object -First 1)) {
        throw "$Path exists and is not a checkout of the core: choose another -Path"
    }
    else {
        & git clone --quiet --branch $Ref $Repository $Path
        if ($LASTEXITCODE -ne 0) { throw "git clone of $Repository ($Ref) failed" }
    }
    Write-Host "  $Path at $(& git -C $Path rev-parse --short HEAD)"

    # ---- Build and install ----
    Write-Title 'Build and install (this takes a few minutes)'
    & (Join-Path $Path 'scripts/build.ps1') -Configuration $Configuration -SkipTests -Install
    # build.ps1 throws on failure; $? covers a script that only returned
    if (-not $?) { throw 'scripts/build.ps1 failed' }

    # ---- Modules ----
    if ($Modules.Count -gt 0) {
        Write-Title 'Modules'
        $organization = $Repository -replace '/[^/]+?(\.git)?$', ''
        foreach ($module in $Modules) {
            # A URL, a local repository, or a name in the organization of the core
            $url = if ($module -match '^(https?://|git@)' -or (Test-Path $module)) { $module } else { "$organization/$module.git" }
            # Only .git goes: module names have dots of their own (Stellar.FileSystem)
            $name = ($url.TrimEnd('/', '\') -split '[/\\]')[-1] -replace '\.git$', ''
            $target = Join-Path $Path "modules/$name"

            if (Test-Path (Join-Path $target '.git')) {
                & git -C $target pull --ff-only --quiet
                Write-Host "  $name updated"
            }
            else {
                & git clone --quiet $url $target
                if ($LASTEXITCODE -ne 0) { throw "git clone of $url failed" }
                Write-Host "  $name cloned"
            }
        }

        & dotnet (Get-ChildItem (Join-Path $Path 'artifacts/bin/Stellar.Cli') -Recurse -Filter 'Stellar.Cli.dll' |
            Select-Object -First 1).FullName modules sync
    }

    # ---- Done ----
    Write-Host "`nStellarEngine is installed in $Path" -ForegroundColor Green
    Write-Host '  Open a new terminal for the stellar command, then:'
    Write-Host '    stellar info'
    Write-Host '    stellar modules new Managed <Module>'
    Write-Host "  Solution of the whole installation: powershell -File `"$Path\scripts\solution.ps1`""
    Write-Host "  Remove it: powershell -File `"$Path\scripts\build.ps1`" -Uninstall"
}

try {
    Install-StellarEngine -Path $Path -Repository $Repository -Ref $Ref -Modules $Modules `
        -Configuration $Configuration -CheckOnly:$CheckOnly
}
catch {
    Write-Host "`nInstallation failed: $($_.Exception.Message)" -ForegroundColor Red
}
