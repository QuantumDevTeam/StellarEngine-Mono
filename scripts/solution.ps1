<#
.SYNOPSIS
    Generates StellarEngine.sln: the core and every module and tool of this installation in one solution.

.DESCRIPTION
    The core has its own solution, StellarEngine-Core.sln, and knows nothing of what lies in modules/ and tools/:
    those are repositories of their own. This script writes the solution of the whole installation for the machine,
    next to the core solution, and the core ignores it (.gitignore). Run it again whenever a module or a project
    appears or goes; the result is the same for the same tree, so an IDE keeps its state.

    StellarEngine.sln = StellarEngine-Core.sln + one solution folder per component:

        modules/<Module>/<every .csproj and .vcxproj of it>
        tools/<Tool>/<every .csproj and .vcxproj of it>

    Folders whose name starts with '_' (modules/_legacy) are left out unless -IncludeHidden.

    Managed projects build as 'Any CPU', native ones as 'x64', for every configuration of the core solution.
    'dotnet sln' cannot be used: it refuses C++ projects without the MSBuild of Visual Studio.

.PARAMETER IncludeHidden
    Also the folders whose name starts with '_', such as modules/_legacy.

.PARAMETER Output
    The solution to write. StellarEngine.sln at the root by default.
#>
param(
    [switch]$IncludeHidden,
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$base = Join-Path $root 'StellarEngine-Core.sln'
if (-not $Output) { $Output = Join-Path $root 'StellarEngine.sln' }

$folderType = '{2150E333-8FDC-42A3-9474-1A3956D46DE8}'
$projectTypes = @{
    '.csproj'  = '{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}'
    '.vcxproj' = '{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}'
}

# The same path gives the same GUID on every run and every machine
$md5 = [System.Security.Cryptography.MD5]::Create()
function Get-StableGuid([string]$Key) {
    $bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("StellarEngine.sln:" + $Key.ToLowerInvariant()))
    return '{' + ([guid]::new($bytes)).ToString().ToUpperInvariant() + '}'
}

function Get-RelativePath([string]$Path) {
    return $Path.Substring($root.Length).TrimStart('\', '/').Replace('/', '\')
}

# The ProjectGuid a native project declares; the solution must use the same one
function Get-ProjectGuid([System.IO.FileInfo]$Project) {
    if ($Project.Extension -eq '.vcxproj') {
        $match = Select-String -Path $Project.FullName -Pattern '<ProjectGuid>\s*(\{[0-9A-Fa-f-]{36}\})\s*</ProjectGuid>' |
            Select-Object -First 1
        if ($match) { return $match.Matches[0].Groups[1].Value.ToUpperInvariant() }
    }
    return Get-StableGuid (Get-RelativePath $Project.FullName)
}

function Find-Projects([string]$Directory) {
    # Not through build output, and not into links
    $stack = [System.Collections.Generic.Stack[System.IO.DirectoryInfo]]::new()
    $stack.Push([System.IO.DirectoryInfo]::new($Directory))
    while ($stack.Count -gt 0) {
        $current = $stack.Pop()
        foreach ($file in $current.EnumerateFiles()) {
            if ($projectTypes.ContainsKey($file.Extension)) { $file }
        }
        foreach ($child in $current.EnumerateDirectories()) {
            if ($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint) { continue }
            if ($child.Name -in 'bin', 'obj', 'artifacts', '.git', '.vs', '.idea', 'node_modules') { continue }
            $stack.Push($child)
        }
    }
}

$text = [System.IO.File]::ReadAllText($base)
$configurations = [regex]::Matches(
    [regex]::Match($text, '(?s)GlobalSection\(SolutionConfigurationPlatforms\) = preSolution(.*?)EndGlobalSection').Groups[1].Value,
    '(?m)^\s*(\S+)\|(\S+) = ') | ForEach-Object { @{ Name = $_.Groups[1].Value; Platform = $_.Groups[2].Value } }
if (-not $configurations) { throw "No solution configurations in $base" }

$projectBlocks = [System.Text.StringBuilder]::new()
$configurationLines = [System.Text.StringBuilder]::new()
$nestingLines = [System.Text.StringBuilder]::new()
$count = 0

foreach ($area in 'modules', 'tools') {
    $areaDirectory = Join-Path $root $area
    if (-not (Test-Path $areaDirectory)) { continue }

    $components = Get-ChildItem $areaDirectory -Directory |
        Where-Object { $IncludeHidden -or -not $_.Name.StartsWith('_') } |
        Where-Object { -not ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) } |
        Sort-Object Name
    $components = @($components | Where-Object { @(Find-Projects $_.FullName).Count -gt 0 })
    if ($components.Count -eq 0) { continue }

    $areaGuid = Get-StableGuid $area
    [void]$projectBlocks.AppendLine("Project(`"$folderType`") = `"$area`", `"$area`", `"$areaGuid`"`r`nEndProject")

    foreach ($component in $components) {
        $componentGuid = Get-StableGuid "$area/$($component.Name)"
        [void]$projectBlocks.AppendLine(
            "Project(`"$folderType`") = `"$($component.Name)`", `"$($component.Name)`", `"$componentGuid`"`r`nEndProject")
        [void]$nestingLines.AppendLine("`t`t$componentGuid = $areaGuid")

        foreach ($project in (Find-Projects $component.FullName | Sort-Object FullName)) {
            $guid = Get-ProjectGuid $project
            $name = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
            [void]$projectBlocks.AppendLine(
                "Project(`"$($projectTypes[$project.Extension])`") = `"$name`", `"$(Get-RelativePath $project.FullName)`", `"$guid`"`r`nEndProject")
            [void]$nestingLines.AppendLine("`t`t$guid = $componentGuid")

            foreach ($configuration in $configurations) {
                $solution = "$($configuration.Name)|$($configuration.Platform)"
                $target = if ($project.Extension -eq '.vcxproj') { "$($configuration.Name)|x64" } else { "$($configuration.Name)|Any CPU" }
                [void]$configurationLines.AppendLine("`t`t$guid.$solution.ActiveCfg = $target")
                [void]$configurationLines.AppendLine("`t`t$guid.$solution.Build.0 = $target")
            }
            $count++
        }
    }
}

function Insert-BeforeEnd([string]$Text, [string]$Section, [string]$Lines) {
    if (-not $Lines) { return $Text }
    $pattern = "(?s)(GlobalSection\($Section\)[^\r\n]*\r?\n.*?)(\tEndGlobalSection)"
    if (-not [regex]::IsMatch($Text, $pattern)) {
        # The core solution has no such section yet: add it before the end of Global
        return [regex]::Replace($Text, '(?m)^EndGlobal', "`tGlobalSection($Section) = preSolution`r`n$Lines`tEndGlobalSection`r`nEndGlobal")
    }
    $evaluator = [System.Text.RegularExpressions.MatchEvaluator] { param($m) $m.Groups[1].Value + $Lines + $m.Groups[2].Value }
    return [regex]::new($pattern).Replace($Text, $evaluator, 1)
}

$text = [regex]::new('(?m)^Global\r?$').Replace($text, $projectBlocks.ToString() + 'Global', 1)
$text = Insert-BeforeEnd $text 'ProjectConfigurationPlatforms' $configurationLines.ToString()
$text = Insert-BeforeEnd $text 'NestedProjects' $nestingLines.ToString()

# Same encoding as the core solution: UTF-8 with a BOM, CRLF
$text = [regex]::Replace($text, '\r?\n', "`r`n")
[System.IO.File]::WriteAllText($Output, $text, [System.Text.UTF8Encoding]::new($true))

Write-Host "$(Split-Path $Output -Leaf): the core and $count project(s) of modules/ and tools/" -ForegroundColor Green
