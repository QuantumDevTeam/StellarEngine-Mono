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

.PARAMETER NoUserRegistration
    With -Install: do not make this the installation of the current user (~/.stellar).
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
    [switch]$NoUserRegistration
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

if ($Install) { $Pack = $true }

# C++/CLI exists on Windows only, and needs the C++ toolchain
$canBuildCli = -not $SkipNative -and $IsWindows -ne $false
$wrappers = if ($canBuildCli) { @('Cli', 'Managed') } else { @('Managed') }

if (-not $Wrapper) { $Wrapper = $wrappers[0] }
if ($wrappers -notcontains $Wrapper) { throw "The $Wrapper wrapper cannot be built here: use -Wrapper Managed" }

if (-not $SkipNative) {
    $msbuild = Find-MSBuild

    # The test project depends on the native core, its P/Invoke exports and the test module
    Invoke-Step 'Native build' {
        & $msbuild (Join-Path $root 'Core/tests/Native/Stellar.Native.Core.Tests/Stellar.Native.Core.Tests.vcxproj') `
            "-p:Configuration=$Configuration" -p:Platform=x64 -m -nologo -v:minimal
    }

    # Built by the MSBuild of Visual Studio: the .NET SDK cannot build C++/CLI
    Invoke-Step 'C++/CLI wrapper' {
        & $msbuild (Join-Path $root 'Core/src/NativeInterop/Stellar.Native.Core.Wrapper.Cli/Stellar.Native.Core.Wrapper.Cli.vcxproj') `
            -restore "-p:Configuration=$Configuration" -p:Platform=x64 -m -nologo -v:minimal
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
}

Write-Host "StellarEngine-Core built ($Configuration, wrappers: $($wrappers -join ', '), packed with: $Wrapper) in $root" -ForegroundColor Green
