<#
.SYNOPSIS
    Installs a version of StellarEngine on a developer machine: checks the toolchain, reads what the orchestra of that
    version is made of, clones the core and every module at their commits, builds and installs it.

.DESCRIPTION
    The one file a machine needs to get the engine. Served from a web server or GitHub Pages:

        irm https://<host>/install.ps1 | iex

    or, with options:

        & ([scriptblock]::Create((irm https://<host>/install.ps1))) -Path D:\StellarEngine -Version 0.8.1

    A version of StellarEngine is a release of the mirror (StellarEngine-Mono): the tag v<version> holds .orchestra,
    the descriptor of the orchestra, with the repository and the commit of the core and of every module. The installer
    reads it and installs exactly that; no list of modules to give.

    Piped into iex a script gets no parameters, so every option also reads an environment variable
    (STELLAR_INSTALL_PATH, STELLAR_INSTALL_VERSION, ...). Nothing is installed into the system by this script: missing
    tools are reported with the command that installs them. The installation itself is scripts/build.ps1 -Install of
    the core (registration in ~/.stellar, the 'stellar' command), undone by scripts/build.ps1 -Uninstall.

.PARAMETER Path
    Where the engine goes: the checkout of the core is the root of the installation. $STELLAR_INSTALL_PATH, or
    ~/StellarEngine.

.PARAMETER Version
    The version of StellarEngine: 0.8.1 or v0.8.1, 'latest' (the latest release, the default), or 'master' (the last
    snapshot of the mirror, released or not). $STELLAR_INSTALL_VERSION.

.PARAMETER Mirror
    The mirror the versions are read from, owner/name on GitHub. $STELLAR_INSTALL_MIRROR, or
    QuantumDevTeam/StellarEngine-Mono.

.PARAMETER Modules
    Modules beyond the orchestra, cloned at their default branch into modules/<Module>/: names in the organization
    of the core, git URLs or local repositories. $STELLAR_INSTALL_MODULES, comma separated.

.PARAMETER Configuration
    Debug or Release. $STELLAR_INSTALL_CONFIGURATION, or Release.

.PARAMETER CheckOnly
    Only checks the toolchain.
#>
param(
    [string]$Path = $env:STELLAR_INSTALL_PATH,
    [string]$Version = $env:STELLAR_INSTALL_VERSION,
    [string]$Mirror = $env:STELLAR_INSTALL_MIRROR,
    [string[]]$Modules = @(),
    [string]$Configuration = $env:STELLAR_INSTALL_CONFIGURATION,
    [switch]$CheckOnly
)

# Everything runs inside a function: piped into iex, an 'exit' would close the shell of the user
function Install-StellarEngine {
    param($Path, $Version, $Mirror, $Modules, $Configuration, $CheckOnly)

    $ErrorActionPreference = 'Stop'
    if (-not $Path) { $Path = Join-Path $HOME 'StellarEngine' }
    if (-not $Version) { $Version = 'latest' }
    if (-not $Mirror) { $Mirror = 'QuantumDevTeam/StellarEngine-Mono' }
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

    # ---- The orchestra ----
    Write-Title 'The orchestra'
    $headers = @{ 'User-Agent' = 'stellar-installer' }
    $ref = switch -Regex ($Version) {
        '^latest$' {
            try { (Invoke-RestMethod "https://api.github.com/repos/$Mirror/releases/latest" -Headers $headers).tag_name }
            catch {
                Write-Host '  No release of StellarEngine yet: installing the last snapshot of the mirror' -ForegroundColor Yellow
                'master'
            }
        }
        '^(master|head)$' { 'master' }
        '^v' { $Version }
        default { "v$Version" }
    }
    try { $descriptor = Invoke-RestMethod "https://raw.githubusercontent.com/$Mirror/$ref/.orchestra" -Headers $headers }
    catch { throw "No StellarEngine $Version`: $Mirror has no .orchestra at '$ref'" }
    if ($descriptor -is [string]) { $descriptor = $descriptor | ConvertFrom-Json }

    $components = @($descriptor.Components)
    $core = $components | Where-Object { -not $_.Area } | Select-Object -First 1
    if (-not $core) { throw "The orchestra at '$ref' of $Mirror names no core" }
    $label = if ($descriptor.Orchestra) { $descriptor.Orchestra } else { $ref }
    Write-Host "  StellarEngine $label ($Mirror at $ref)"
    foreach ($component in $components) {
        $where = if ($component.Area) { "$($component.Area)/$($component.Name)" } else { '(root)' }
        Write-Host ('    {0,-28} {1,-10} {2}  {3}' -f $component.Name, $component.Version, "$($component.Commit)".Substring(0, 10), $where)
    }

    # Clones a component, or updates the clone that is there, and puts it at its commit of the orchestra
    function Sync-Component([string]$Name, [string]$Url, [string]$Target, [string]$Commit) {
        if (Test-Path (Join-Path $Target '.git')) {
            if (& git -C $Target status --porcelain --untracked-files=no) {
                throw "$Name has uncommitted changes in $Target`: commit or stash them first"
            }
            & git -C $Target fetch --quiet --tags origin
            if ($LASTEXITCODE -ne 0) { throw "git fetch of $Name failed" }
        }
        else {
            # No prompt for a repository this machine cannot read: stored credentials work, a login window never opens
            $env:GIT_TERMINAL_PROMPT = '0'
            & git -c credential.interactive=never clone --quiet $Url $Target
            if ($LASTEXITCODE -ne 0) { return $false }
        }
        if ($Commit) {
            # A commit of the orchestra, not a branch: what this version is made of (git switch <branch> to work on it)
            & git -C $Target -c advice.detachedHead=false checkout --quiet $Commit
            if ($LASTEXITCODE -ne 0) { throw "$Name has no commit $Commit in $Url`: was it pushed?" }
        }
        return $true
    }

    # ---- The core ----
    Write-Title "The core: $($core.Repository)"
    $Path = [System.IO.Path]::GetFullPath($Path)
    $isCore = Test-Path (Join-Path $Path 'Shared/Build/Stellar.Versions.props')
    if (-not $isCore -and (Test-Path $Path) -and (Get-ChildItem $Path -Force | Select-Object -First 1)) {
        throw "$Path exists and is not a checkout of the core: choose another -Path"
    }
    if (-not (Sync-Component $core.Name $core.Repository $Path $core.Commit)) { throw "git clone of $($core.Repository) failed" }
    Write-Host "  $Path at $(& git -C $Path rev-parse --short HEAD)"

    # ---- Build and install ----
    Write-Title 'Build and install (this takes a few minutes)'
    & (Join-Path $Path 'scripts/build.ps1') -Configuration $Configuration -SkipTests -Install
    # build.ps1 throws on failure; $? covers a script that only returned
    if (-not $?) { throw 'scripts/build.ps1 failed' }

    # ---- Modules ----
    $skipped = [System.Collections.Generic.List[string]]::new()
    $others = @($components | Where-Object { $_.Area })
    if ($others.Count -gt 0 -or $Modules.Count -gt 0) {
        Write-Title 'Modules and tools'
        foreach ($component in $others) {
            $target = Join-Path $Path "$($component.Area)/$($component.Name)"
            if (Sync-Component $component.Name $component.Repository $target $component.Commit) {
                Write-Host "  $($component.Name) $($component.Version)"
            }
            else {
                # A private repository this machine cannot read: the rest of the orchestra works without it
                Write-Host "  $($component.Name) skipped: $($component.Repository) cannot be cloned (private?)" -ForegroundColor Yellow
                $skipped.Add($component.Name)
            }
        }

        # Beyond the orchestra: at their default branch
        $organization = $core.Repository -replace '/[^/]+?(\.git)?$', ''
        foreach ($module in $Modules) {
            # A URL, a local repository, or a name in the organization of the core
            $url = if ($module -match '^(https?://|git@)' -or (Test-Path $module)) { $module } else { "$organization/$module.git" }
            # Only .git goes: module names have dots of their own (Stellar.FileSystem)
            $name = ($url.TrimEnd('/', '\') -split '[/\\]')[-1] -replace '\.git$', ''
            if (-not (Sync-Component $name $url (Join-Path $Path "modules/$name") '')) { throw "git clone of $url failed" }
            Write-Host "  $name (beyond the orchestra)"
        }

        & dotnet (Get-ChildItem (Join-Path $Path 'artifacts/bin/Stellar.Cli') -Recurse -Filter 'Stellar.Cli.dll' |
            Select-Object -First 1).FullName modules sync
    }

    # ---- Done ----
    Write-Host "`nStellarEngine $label is installed in $Path" -ForegroundColor Green
    if ($skipped.Count -gt 0) { Write-Host "  Not installed (no access): $($skipped -join ', ')" -ForegroundColor Yellow }
    Write-Host '  The core and the modules are at the commits of this version: git switch <branch> in one to work on it.'
    Write-Host '  Open a new terminal for the stellar command, then:'
    Write-Host '    stellar info'
    Write-Host '    stellar modules new Managed <Module>'
    Write-Host "  Solution of the whole installation: powershell -File `"$Path\scripts\solution.ps1`""
    Write-Host "  Remove it: powershell -File `"$Path\scripts\build.ps1`" -Uninstall"
}

try {
    Install-StellarEngine -Path $Path -Version $Version -Mirror $Mirror -Modules $Modules `
        -Configuration $Configuration -CheckOnly:$CheckOnly
}
catch {
    Write-Host "`nInstallation failed: $($_.Exception.Message)" -ForegroundColor Red
}
