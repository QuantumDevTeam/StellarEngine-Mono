<#
.SYNOPSIS
    Builds the core from sources: native, managed, tests, packages; and makes this checkout an installation.

.DESCRIPTION
    This is the script a target machine runs: the engine ships as sources, and this checkout of StellarEngine-Core
    is the root of the installation ($STELLAR_ROOT). Everything built goes to artifacts/bin and artifacts/obj,
    managed and native alike; packages go to packages/.

    The native core has two wrappers with the same classes, and Stellar.Sdk picks one (StellarNativeWrapper):
      Cli      C++/CLI over the C++ classes, the default on Windows
      Managed  C# over the P/Invoke exports, everywhere else and for Native AOT
    The managed tests run once per wrapper that was built, and a parity test checks both expose the same API.

.PARAMETER Configuration
    Debug or Release.

.PARAMETER Wrapper
    The wrapper Stellar.Core is packed with. By default the one the SDK picks on this machine.

.PARAMETER SkipNative
    Builds only the managed part, for machines with no C++ toolchain: only the Managed wrapper is built, and the
    tests that need the native libraries are skipped.

.PARAMETER SkipTests
    Skips the tests.

.PARAMETER Pack
    Also fills the local feed, packages/ of this checkout.

.PARAMETER Install
    Also runs 'stellar install' here: modules/, manifests/, config/ and the rest of an installation. Implies -Pack.
    Registers the installation for the current user and installs the 'stellar' command (a global dotnet tool).

.PARAMETER NoUserRegistration
    With -Install: do not register this installation for the current user ($STELLAR_HOME or ~/.stellar), and do
    not install the 'stellar' command. For sandboxes.

.PARAMETER Uninstall
    Undoes -Install and the build instead of building: 'stellar uninstall' here (the registration of the user, the
    packages of this installation in the NuGet cache, what the installer created), the 'stellar' command, then
    artifacts/. Modules, tools and games in their folders are never touched.

.PARAMETER All
    With -Uninstall: also the files you may have edited (config/, the isolation files, the engine manifest).

.PARAMETER KeepArtifacts
    With -Uninstall: keep the build output, artifacts/.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [ValidateSet('', 'Cli', 'Managed')]
    [string]$Wrapper = '',
    [switch]$SkipNative,
    [switch]$SkipTests,
    [switch]$Pack,
    [switch]$Install,
    [switch]$NoUserRegistration,
    [switch]$Uninstall,
    [switch]$All,
    [switch]$KeepArtifacts
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

function Invoke-Step {
    param([string]$Title, [scriptblock]$Action)

    Write-Host "== $Title" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Title failed with exit code $LASTEXITCODE" }
}

function Find-MSBuild {
    if ($env:STELLAR_MSBUILD -and (Test-Path $env:STELLAR_MSBUILD)) { return $env:STELLAR_MSBUILD }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) { throw 'vswhere.exe not found: install Visual Studio with the C++ workload, or use -SkipNative' }

    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild not found: install Visual Studio with the C++ workload, or use -SkipNative' }

    return $msbuild
}

# Whether Visual Studio can build the C++/CLI wrapper. Without it the engine works the same over P/Invoke
function Test-CppCliSupport {
    if ($IsWindows -eq $false) { return $false }
    if ($env:STELLAR_CPPCLI) { return $env:STELLAR_CPPCLI -eq '1' }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) { return $false }

    $found = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.CLI.Support -property installationPath
    return [bool]$found
}

# The 'stellar' command: Stellar.Cli as a global dotnet tool ($DOTNET_CLI_HOME moves it, for sandboxes)
function Test-StellarTool {
    return [bool](dotnet tool list -g | Select-String -Pattern '^stellar\.cli\s' -Quiet)
}

function Uninstall-StellarTool {
    if (Test-StellarTool) {
        Invoke-Step "Uninstall the 'stellar' command" { dotnet tool uninstall -g Stellar.Cli }
    }
}

function Install-StellarTool {
    $package = Get-ChildItem (Join-Path $root 'packages') -Filter 'Stellar.Cli.*.nupkg' |
        Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $package) { throw 'No Stellar.Cli package in packages/' }
    $version = $package.BaseName.Substring('Stellar.Cli.'.Length)

    Uninstall-StellarTool

    # A package of the same version left in the NuGet cache would win over the one just packed
    $cache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }
    $cached = Join-Path $cache "stellar.cli/$($version.ToLowerInvariant())"
    if (Test-Path $cached) { [System.IO.Directory]::Delete($cached, $true) }

    # From the root, so the NuGet.config of the checkout maps Stellar.* to packages/
    Push-Location $root
    try {
        Invoke-Step "Install the 'stellar' command ($version)" {
            dotnet tool install -g Stellar.Cli --version $version --add-source (Join-Path $root 'packages')
        }
    }
    finally { Pop-Location }
}

if ($Uninstall) {
    if ($Install -or $Pack) { throw '-Uninstall cannot be combined with -Install or -Pack' }

    $arguments = @('uninstall', $root)
    if ($All) { $arguments += '--all' }

    # Built if needed; it runs from artifacts/, so artifacts/ goes only after it exits
    Invoke-Step 'Uninstall' {
        dotnet run --project (Join-Path $root 'SDK/src/Stellar.Cli/Stellar.Cli.csproj') -c $Configuration -v quiet -- @arguments
    }

    Uninstall-StellarTool

    $artifacts = Join-Path $root 'artifacts'
    if (-not $KeepArtifacts -and (Test-Path $artifacts)) {
        dotnet build-server shutdown | Out-Null
        # Not Remove-Item -Recurse: Windows PowerShell walks into junctions; Directory.Delete removes them as links
        [System.IO.Directory]::Delete($artifacts, $true)
        Write-Host "Removed $artifacts"
    }

    Write-Host "StellarEngine-Core uninstalled from ${root}: a plain checkout again" -ForegroundColor Green
    return
}

if ($Install) { $Pack = $true }

# Stellar.Core carries the native core in its package: there is nothing to pack without it
if ($SkipNative -and $Pack) { throw '-Pack and -Install need the native core: drop -SkipNative (Visual Studio with the C++ workload)' }

# C++/CLI exists on Windows only, and needs the C++/CLI component of Visual Studio on top of the C++ toolchain
$canBuildCli = -not $SkipNative -and (Test-CppCliSupport)
if (-not $SkipNative -and -not $canBuildCli) {
    Write-Host 'C++/CLI support not found in Visual Studio: the C++/CLI wrapper is not built, the engine uses the P/Invoke wrapper (Managed). Add "C++/CLI support" in the Visual Studio Installer to get both.' -ForegroundColor Yellow
}
# @(): a one-element array out of an if would be unrolled into a string
$wrappers = @(if ($canBuildCli) { 'Cli', 'Managed' } else { 'Managed' })

if (-not $Wrapper) { $Wrapper = $wrappers[0] }
if ($wrappers -notcontains $Wrapper) { throw "The $Wrapper wrapper cannot be built here (no C++/CLI support): use -Wrapper Managed" }

if (-not $SkipNative) {
    $msbuild = Find-MSBuild

    # The test project depends on the native core, its P/Invoke exports and the test module
    Invoke-Step 'Native build' {
        & $msbuild (Join-Path $root 'Core/tests/Native/Stellar.Native.Core.Tests/Stellar.Native.Core.Tests.vcxproj') `
            "-p:Configuration=$Configuration" -p:Platform=x64 -m -nologo -v:minimal
    }

    # Built by the MSBuild of Visual Studio: the .NET SDK cannot build C++/CLI
    if ($canBuildCli) {
        Invoke-Step 'C++/CLI wrapper' {
            & $msbuild (Join-Path $root 'Core/src/NativeInterop/Stellar.Native.Core.Wrapper.Cli/Stellar.Native.Core.Wrapper.Cli.vcxproj') `
                -restore "-p:Configuration=$Configuration" -p:Platform=x64 -m -nologo -v:minimal
        }
    }

    if (-not $SkipTests) {
        Invoke-Step 'Native tests' {
            & (Join-Path $root "artifacts/bin/Stellar.Native.Core.Tests/$Configuration/Stellar.Native.Core.Tests.exe")
        }
    }
}

$managed = Join-Path $root 'StellarEngine-Core.Managed.slnf'

# Projects that depend on the wrapper build into bin/<wrapper>/, so the variants live side by side
foreach ($variant in $wrappers) {
    Invoke-Step "Managed build ($variant)" {
        dotnet build $managed -c $Configuration "-p:StellarNativeWrapper=$variant" -v minimal --nologo
    }

    if (-not $SkipTests) {
        Invoke-Step "Managed tests ($variant)" {
            dotnet test $managed -c $Configuration "-p:StellarNativeWrapper=$variant" --no-build --nologo
        }
    }
}

if ($Pack) {
    $projects = 'Kernel/src/Managed/Stellar.Kernel', 'Core/src/Managed/Stellar.Core', 'Runtime/src/Managed/Stellar.Runtime', 'SDK/src/Stellar.Tools', 'SDK/src/Stellar.Cli', 'SDK/src/MSBuild/Stellar.Sdk'

    foreach ($project in $projects) {
        $csproj = Join-Path $root "$project/$(Split-Path $project -Leaf).csproj"
        Invoke-Step "Pack $(Split-Path $project -Leaf)" {
            dotnet pack $csproj -c $Configuration "-p:StellarNativeWrapper=$Wrapper" -v quiet --nologo
        }
    }
}

if ($Install) {
    $arguments = @('install', $root)
    if ($NoUserRegistration) { $arguments += '--no-user-registration' }

    Invoke-Step 'Install' {
        dotnet run --project (Join-Path $root 'SDK/src/Stellar.Cli/Stellar.Cli.csproj') -c $Configuration --no-build -- @arguments
    }

    # Registration and the command go together: both belong to the user, not to the checkout
    if (-not $NoUserRegistration) { Install-StellarTool }
}

Write-Host "StellarEngine-Core built ($Configuration, wrappers: $($wrappers -join ', '), packed with: $Wrapper) in $root" -ForegroundColor Green
