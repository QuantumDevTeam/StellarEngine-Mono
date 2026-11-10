<#
.SYNOPSIS
    End-to-end check of an installation: a copy of this checkout becomes the root, gets built and installed in place,
    gets two modules from the templates and a game on top of them, once per wrapper of the native core.

.DESCRIPTION
    What it goes through, as a user would:
      1. the sources are copied into a fresh sandbox, as 'git clone' would put them;
      2. scripts/build.ps1 -Install builds the core there and makes it an installation;
      3. 'stellar modules new' creates a managed and a native module, a dependency goes into a descriptor;
      4. 'stellar modules build' builds both, with their tests and packages;
      5. 'stellar project new' creates a game that uses a module, built against sources (Debug) and packages (Release);
      6. what must be rejected is: an unknown entry point, an unknown module, an unknown wrapper, a broken level rule,
         a manifest change that breaks a dependency.

    Nothing outside the sandbox is changed: it has its own package cache, the user pointer (~/.stellar) is not written,
    and no links are created (recursive deletion in Windows PowerShell walks through junctions).

.PARAMETER Wrapper
    The wrappers to check. Both by default.

.PARAMETER KeepSandbox
    Leave the sandbox for inspection.
#>
param(
    [ValidateSet('Cli', 'Managed')]
    [string[]]$Wrapper = @('Cli', 'Managed'),
    [switch]$KeepSandbox
)

$ErrorActionPreference = 'Stop'
$env:MSBUILDDISABLENODEREUSE = '1'

$repository = Split-Path $PSScriptRoot -Parent
$root = Join-Path ([IO.Path]::GetTempPath()) "stellar-root-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

function Invoke-Checked {
    param([string]$Title, [scriptblock]$Action)

    Write-Host "== $Title" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Title failed with exit code $LASTEXITCODE" }
}

function Stellar {
    dotnet run --project (Join-Path $root 'SDK/src/Stellar.Cli/Stellar.Cli.csproj') -c Release --no-build -- @args
    if ($LASTEXITCODE -ne 0) { throw "stellar $args failed with exit code $LASTEXITCODE" }
}

# Runs a build that must fail, and returns its output: native stderr must not stop the script
function Get-FailedBuild {
    param([string[]]$Arguments)

    $log = Join-Path $root 'expected-failure.log'
    cmd /c "dotnet $($Arguments -join ' ') > `"$log`" 2>&1"
    $failed = $LASTEXITCODE -ne 0
    return @{ Failed = $failed; Text = (Get-Content $log -Raw) }
}

# 1. The sources, as a clone would put them
$skipAtRoot = 'artifacts', 'packages', 'modules', 'manifests', 'projects', 'config', 'tools'
New-Item -ItemType Directory -Path $root | Out-Null
Get-ChildItem $repository -Force | Where-Object { $skipAtRoot -notcontains $_.Name -and $_.Name -notin '.git', '.idea', '.vs' } | ForEach-Object {
    $target = Join-Path $root $_.Name
    if ($_.PSIsContainer) {
        robocopy $_.FullName $target /E /XD bin obj .vs .idea /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "copy of $($_.Name) failed" }
    }
    else {
        Copy-Item $_.FullName $target
    }
}
$global:LASTEXITCODE = 0

$env:NUGET_PACKAGES = Join-Path $root '.nuget'
Remove-Item Env:STELLAR_ROOT -ErrorAction SilentlyContinue

try {
    # 2. Built and installed in place. Debug too: modules build against the Debug native core from sources
    Invoke-Checked 'Build and install (Release)' { & (Join-Path $root 'scripts/build.ps1') -Configuration Release -SkipTests -Install -NoUserRegistration }
    Invoke-Checked 'Build (Debug)' { & (Join-Path $root 'scripts/build.ps1') -Configuration Debug -SkipTests }

    Push-Location $root
    try {
        # 3. Two modules from the templates; the dependency is declared once, in the descriptor
        Stellar modules new Managed Stellar.Sample --level CoreModule -d 'A sample core system' | Out-Null
        Stellar modules new Native Stellar.Visual --level HighLevel | Out-Null

        $descriptor = Join-Path $root 'modules/Stellar.Visual/Stellar.Visual.stellar.module'
        Set-Content $descriptor (Get-Content $descriptor -Raw).Replace('"Dependencies": []', '"Dependencies": ["Stellar.Sample"]') -NoNewline
        Stellar modules sync | Out-Null

        $game = Join-Path $root 'projects/Demo'
        Stellar project new Game Demo | Out-Null
        $gameProject = Join-Path $game 'Demo.csproj'
        Set-Content $gameProject (Get-Content $gameProject -Raw).Replace('</Project>', '<ItemGroup><StellarDependency Include="Stellar.Visual"/></ItemGroup></Project>')
        $entryPoint = Join-Path $game 'EntryPoint.cs'
        Set-Content $entryPoint (Get-Content $entryPoint -Raw).Replace('return 0;',
            'Console.WriteLine($"module={Stellar.Visual.VisualModule.Id} context={System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Stellar.Visual.VisualModule).Assembly)?.Name} wrapper={Stellar.Core.Native.NativeCore.Wrapper} id={Stellar.Core.Identifier.Create().ToCanonicalString()}"); return 0;')

        # The game runs its main loop until it is stopped: three frames are enough here
        function Invoke-Game {
            param([string]$Output)

            $text = & (Join-Path $Output 'Demo.exe') '--stellar:MaxFrames=3' 2>&1 | Out-String
            if ($LASTEXITCODE -ne 0) { throw "Demo.exe exited with $LASTEXITCODE`n$text" }
            if ($text -notmatch "module=Stellar.Visual context=Stellar.Module:Stellar.Visual wrapper=$variant id=[0-9a-f-]{36}") { throw "Unexpected output: $text" }
            return $text.Trim() -replace '\s+', ' '
        }

        foreach ($variant in $Wrapper) {
            # The Stellar.Core package carries the wrapper it was built with: one package cache per wrapper
            $env:NUGET_PACKAGES = Join-Path $root ".nuget-$variant"
            Invoke-Checked "[$variant] Pack Stellar.Core" {
                dotnet pack (Join-Path $root 'Core/src/Managed/Stellar.Core/Stellar.Core.csproj') -c Release "-p:StellarNativeWrapper=$variant" -v quiet --nologo
            }

            # Everything built lands in artifacts/: the projects of the modules and of the game start from scratch
            foreach ($tree in 'bin', 'obj') {
                Get-ChildItem (Join-Path $root "artifacts/$tree") -Directory |
                    Where-Object { $_.Name -like 'Stellar.Sample*' -or $_.Name -like 'Stellar.Visual*' -or $_.Name -like 'Stellar.Native.Visual*' -or $_.Name -eq 'Demo' } |
                    Remove-Item -Recurse -Force
            }

            # 4. Both modules, native part first, with tests and packages
            Invoke-Checked "[$variant] Build the modules" { Stellar modules build --tests --pack --wrapper $variant }

            # 5. The game: sources in Debug, packages in Release
            foreach ($configuration in 'Debug', 'Release') {
                Invoke-Checked "[$variant] Build the game ($configuration)" { dotnet build $gameProject -c $configuration "-p:StellarNativeWrapper=$variant" -v quiet --nologo }

                $output = Join-Path $root "artifacts/bin/Demo/$configuration"
                foreach ($file in 'Demo.exe', 'Demo.dll', 'Demo.Launcher.dll', 'Demo.Launcher.deps.json', 'Demo.Launcher.runtimeconfig.json', 'Demo.stellar.runtime.config.json', 'Stellar.Runtime.dll', 'Stellar.Native.Visual.dll', 'Stellar.Sample.dll') {
                    if (-not (Test-Path (Join-Path $output $file))) { throw "$configuration build did not produce $file" }
                }

                $assets = Get-Content (Join-Path $root 'artifacts/obj/Demo/project.assets.json') -Raw | ConvertFrom-Json
                $visual = $assets.libraries.PSObject.Properties | Where-Object { $_.Name -like 'Stellar.Visual/*' }
                $expected = if ($configuration -eq 'Debug') { 'project' } else { 'package' }
                if ($visual.Value.type -ne $expected) { throw "$configuration links Stellar.Visual as '$($visual.Value.type)', expected '$expected'" }

                $text = Invoke-Game $output
                Write-Host "   $configuration, Stellar.Visual as a ${expected}: $text"
            }

            # 6. The runtime configuration inside the game: no file next to it, the assembly says where it is
            Invoke-Checked "[$variant] Build the game (Debug, embedded configuration)" {
                dotnet build $gameProject -c Debug "-p:StellarNativeWrapper=$variant" -p:StellarRuntimeConfigType=Embedded -v quiet --nologo
            }
            $output = Join-Path $root 'artifacts/bin/Demo/Debug'
            Remove-Item (Join-Path $output 'Demo.stellar.runtime.config.json') -ErrorAction SilentlyContinue
            $text = Invoke-Game $output
            Write-Host "   Debug, embedded configuration: $text"
        }

        # 7. What must be rejected
        Write-Host '== Rejections' -ForegroundColor Cyan

        $configurationFile = Join-Path $game 'Demo.stellar.project'
        $goodConfiguration = Get-Content $configurationFile -Raw
        Set-Content $configurationFile $goodConfiguration.Replace('"default"', '"Demo.Missing"')
        $result = Get-FailedBuild @('build', "`"$gameProject`"", '-c', 'Debug')
        Set-Content $configurationFile $goodConfiguration -NoNewline
        if (-not $result.Failed -or $result.Text -notmatch 'STLR0101') { throw "Expected STLR0101:`n$($result.Text)" }
        Write-Host '   an unknown entry point: STLR0101'

        $goodProject = Get-Content $gameProject -Raw
        Set-Content $gameProject $goodProject.Replace('</Project>', '<ItemGroup><StellarDependency Include="Stellar.Nothing"/></ItemGroup></Project>')
        $result = Get-FailedBuild @('build', "`"$gameProject`"", '-c', 'Debug')
        Set-Content $gameProject $goodProject -NoNewline
        if (-not $result.Failed -or $result.Text -notmatch 'Unknown Stellar modules: Stellar\.Nothing') { throw "Expected the unknown module error:`n$($result.Text)" }
        Write-Host '   an unknown module'

        $result = Get-FailedBuild @('build', "`"$gameProject`"", '-c', 'Debug', '-p:StellarNativeWrapper=Nothing')
        if (-not $result.Failed -or $result.Text -notmatch "Unknown StellarNativeWrapper 'Nothing'") { throw "Expected the unknown wrapper error:`n$($result.Text)" }
        Write-Host '   an unknown wrapper'

        Stellar modules new Managed Stellar.Probe --level CoreModule | Out-Null
        $probe = Join-Path $root 'modules/Stellar.Probe/src/Managed/Stellar.Probe/Stellar.Probe.csproj'
        Set-Content $probe (Get-Content $probe -Raw).Replace('</Project>', '<ItemGroup><StellarDependency Include="Stellar.Visual"/></ItemGroup></Project>')
        $result = Get-FailedBuild @('build', "`"$probe`"", '-c', 'Debug')
        if (-not $result.Failed -or $result.Text -notmatch 'STLR0201' -or $result.Text -notmatch 'STLR0202') { throw "Expected STLR0201 and STLR0202:`n$($result.Text)" }
        Write-Host '   a core system that uses a high-level module: STLR0201, STLR0202'

        $manifest = Join-Path $root 'manifests/engine.manifest.json'
        $before = Get-Content $manifest -Raw
        cmd /c "dotnet run --project `"$(Join-Path $root 'SDK/src/Stellar.Cli/Stellar.Cli.csproj')`" -c Release --no-build -- modules disable Stellar.Sample > nul 2>&1"
        if ($LASTEXITCODE -eq 0) { throw 'Turning off a module others depend on was accepted' }
        if ((Get-Content $manifest -Raw) -ne $before) { throw 'A rejected manifest change was kept' }
        Write-Host '   turning off a module others depend on, and the manifest is left as it was'
    }
    finally {
        Pop-Location
    }

    Write-Host "End-to-end check passed for: $($Wrapper -join ', ')" -ForegroundColor Green

    # The last program run here was meant to fail: that is not the result of the check
    $global:LASTEXITCODE = 0
}
finally {
    if ($KeepSandbox) {
        Write-Host "Sandbox kept: $root"
    }
    elseif (Test-Path $root) {
        # No reparse points are ever created in the sandbox, so a recursive delete stays inside it
        Remove-Item $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}
