<#
.SYNOPSIS
    Makes a snapshot of the whole orchestra (the core, every module and tool) as one commit of the mirror repository.

.DESCRIPTION
    Every component is a repository of its own, nested in one folder on the machine:

        <root>/                    StellarEngine-Core
        <root>/modules/<Module>/   one repository each
        <root>/tools/<Tool>/       one repository each

    The mirror (StellarEngine-Mono) is not worked in: it only shows the orchestra as a whole. This script takes the
    COMMITTED state (HEAD) of each component and makes it one commit of the mirror, the core at the root and each
    component in its folder. The histories stay in the components; the mirror gets one commit per snapshot, on top
    of its own history, and its message tells which commit of each component went in. Uncommitted changes are not in
    a snapshot: the script warns about them.

    Nothing of the working folders is touched. The mirror lives in its own bare repository, <root>/.mirror (ignored
    by the core), and the commit is built with git plumbing only.

    Without -Push the commit stays local and the script prints the command that publishes it.

.PARAMETER Remote
    The mirror repository.

.PARAMETER Branch
    The branch of the mirror the snapshot goes on.

.PARAMETER Message
    The first line of the commit. 'Orchestra snapshot <date>' by default.

.PARAMETER Tag
    Also tags the snapshot, e.g. v0.8.0 for a release of the orchestra.

.PARAMETER Push
    Publishes the snapshot (and the tag).

.PARAMETER IncludeHidden
    Also the components whose folder name starts with '_'.

.PARAMETER Keep
    Components to take as they are in the last snapshot of the mirror, folder and entry of .orchestra alike, for
    those whose repository cannot be read here (a private one, in the mirror workflow without a token for it).
#>
param(
    [string]$Remote = 'https://github.com/QuantumDevTeam/StellarEngine-Mono.git',
    [string]$Branch = 'master',
    [string]$Message = '',
    [string]$Tag = '',
    [switch]$Push,
    [switch]$IncludeHidden,
    [string[]]$Keep = @()
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$mirror = Join-Path $root '.mirror'
# Git speaks UTF-8: file names in its output must not go through the console code page
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

function Invoke-Git {
    $output = & git @args
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed with exit code $LASTEXITCODE" }
    return $output
}

function Invoke-Mirror { Invoke-Git --git-dir $mirror @args }

# A git call allowed to fail (a branch that does not exist yet): its exit code says so, its stderr is not an error.
# Windows PowerShell turns the stderr of a native command into an error record under -ErrorAction Stop
function Invoke-GitQuiet {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { & git @args 2>$null }
    finally { $ErrorActionPreference = $previous }
}

# ---- The components ----
$components = [System.Collections.Generic.List[object]]::new()
$components.Add(@{ Name = 'StellarEngine-Core'; Area = ''; Path = $root })
foreach ($area in 'modules', 'tools') {
    $directory = Join-Path $root $area
    if (-not (Test-Path $directory)) { continue }

    Get-ChildItem $directory -Directory | Sort-Object Name | ForEach-Object {
        if (-not $IncludeHidden -and $_.Name.StartsWith('_')) { return }
        # tools/compilers belongs to the installer (resource compilers of this installation), not to a component
        if ($area -eq 'tools' -and $_.Name -eq 'compilers') { return }
        if (-not (Test-Path (Join-Path $_.FullName '.git'))) {
            Write-Warning "$area/$($_.Name) is not a repository: left out"
            return
        }
        $components.Add(@{ Name = $_.Name; Area = $area; Path = $_.FullName })
    }
}

# ---- The mirror repository ----
if (-not (Test-Path $mirror)) {
    Invoke-Git init --bare --quiet $mirror | Out-Null
    Invoke-Mirror remote add origin $Remote | Out-Null
}
elseif ((Invoke-Mirror remote get-url origin) -ne $Remote) {
    Invoke-Mirror remote set-url origin $Remote | Out-Null
}

# The history of the mirror, if it has one yet
Invoke-GitQuiet --git-dir $mirror fetch --quiet --no-tags origin "+refs/heads/${Branch}:refs/remotes/origin/$Branch" | Out-Null
$parent = Invoke-GitQuiet --git-dir $mirror rev-parse --verify --quiet "refs/remotes/origin/$Branch"
$local = Invoke-GitQuiet --git-dir $mirror rev-parse --verify --quiet "refs/heads/$Branch"
if ($local -and $local -ne $parent) {
    # A snapshot made before and not pushed yet: build on it, unless the remote has gone past it already
    $behind = $false
    if ($parent) {
        Invoke-GitQuiet --git-dir $mirror merge-base --is-ancestor $local $parent | Out-Null
        $behind = $LASTEXITCODE -eq 0
    }
    if (-not $behind) { $parent = $local }
}

# ---- The snapshot ----
# Built from objects only (git mktree): a bare repository has no index or work tree to read the trees into
function New-Tree([string[]]$Entries) {
    # Through a file: a PowerShell pipe into a native command ends lines with CRLF, and mktree would keep the CR in
    # the names. cmd's '<' passes the bytes as they are
    $file = Join-Path $mirror 'snapshot.tree'
    [System.IO.File]::WriteAllText($file, (($Entries | ForEach-Object { "$_" }) -join "`n") + "`n",
        [System.Text.UTF8Encoding]::new($false))
    try {
        # The shell's '<' passes the bytes as they are: cmd on Windows, sh elsewhere (the mirror workflow runs on Linux)
        $sha = if ($env:OS -eq 'Windows_NT') { & cmd /c "git --git-dir `"$mirror`" mktree < `"$file`"" }
               else { & sh -c 'git --git-dir "$1" mktree < "$2"' sh $mirror $file }
        if ($LASTEXITCODE -ne 0) { throw 'git mktree failed' }
    }
    finally { Remove-Item $file }
    return "$sha".Trim()
}

$rootEntries = @()
$areaEntries = [ordered]@{ modules = @(); tools = @() }
$lines = [System.Collections.Generic.List[string]]::new()
$marker = [System.Collections.Generic.List[object]]::new()

foreach ($component in $components) {
    $head = Invoke-GitQuiet -C $component.Path rev-parse --verify --quiet HEAD
    if (-not $head) {
        Write-Warning "$($component.Name) has no commits yet: left out"
        continue
    }

    $dirty = & git -C $component.Path status --porcelain --untracked-files=no
    if ($dirty) { Write-Warning "$($component.Name) has uncommitted changes: the snapshot takes its last commit only" }

    $reference = "refs/components/$($component.Name)"
    Invoke-Mirror fetch --quiet --no-tags $component.Path "+HEAD:$reference" | Out-Null
    $componentTree = "$(Invoke-Mirror rev-parse "$reference^{tree}")".Trim()

    if ($component.Area) { $areaEntries[$component.Area] += "040000 tree $componentTree`t$($component.Name)" }
    else { $rootEntries = @(Invoke-Mirror -c core.quotePath=false ls-tree $componentTree) }

    $branch = "$(& git -C $component.Path rev-parse --abbrev-ref HEAD)".Trim()
    $subject = "$(& git -C $component.Path log -1 --format=%s HEAD)".Trim()
    $lines.Add(('- {0} {1} ({2}): {3}' -f $component.Name, $head.Substring(0, 10), $branch, $subject))
    $repository = "$(Invoke-GitQuiet -C $component.Path remote get-url origin)".Trim()
    # A URL may carry credentials (https://user:token@host/...): never into a file of a public repository
    $repository = $repository -replace '^(https?://)[^@/]+@', '$1'
    # No branch: a machine snapshots what it has checked out, the workflow the default branch; the commit is the fact
    $marker.Add([ordered]@{ Name = $component.Name; Area = $component.Area; Commit = "$head".Trim(); Repository = $repository })
}

# Components kept as the last snapshot has them: the same folder, the same entry of .orchestra
if ($Keep.Count -gt 0) {
    if (-not $parent) { throw "-Keep needs a previous snapshot, and the mirror has none on $Branch" }
    $previous = @((Invoke-Mirror show "${parent}:.orchestra" | Out-String | ConvertFrom-Json).Components)

    foreach ($name in $Keep) {
        if ($marker | Where-Object { $_.Name -eq $name }) { throw "$name is both here and in -Keep" }
        $entry = $previous | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        if (-not $entry -or -not $entry.Area) { throw "$name is not a component of the last snapshot: it cannot be kept" }

        $keptTree = "$(Invoke-Mirror rev-parse "${parent}:$($entry.Area)/$name")".Trim()
        $areaEntries[$entry.Area] += "040000 tree $keptTree`t$name"
        $marker.Add([ordered]@{ Name = $entry.Name; Area = $entry.Area; Commit = $entry.Commit; Repository = $entry.Repository })
        $lines.Add(('- {0} {1}: kept from the last snapshot' -f $name, "$($entry.Commit)".Substring(0, 10)))
    }
}

# .orchestra at the root: tells the tree is a mirror (the docs workflow builds the orchestra site then) and what it
# is made of. Content only: no date, so an unchanged orchestra gives the same tree
$marker = @($marker | Sort-Object { $_.Area }, { $_.Name })
# Written by hand, not with ConvertTo-Json: Windows PowerShell and PowerShell 7 format it differently, and the same
# orchestra snapshotted on a machine and in the workflow must give the same bytes
function ConvertTo-JsonString([string]$Value) {
    return '"' + ($Value -replace '\\', '\\' -replace '"', '\"') + '"'
}
$markerLines = foreach ($entry in $marker) {
    '    { ' + (@('Name', 'Area', 'Commit', 'Repository' | ForEach-Object {
                (ConvertTo-JsonString $_) + ': ' + (ConvertTo-JsonString "$($entry[$_])")
            }) -join ', ') + ' }'
}
$markerJson = "{`n  `"Components`": [`n" + ($markerLines -join ",`n") + "`n  ]`n}`n"
$markerFile = Join-Path $mirror 'snapshot.orchestra'
[System.IO.File]::WriteAllText($markerFile, $markerJson, [System.Text.UTF8Encoding]::new($false))
try { $markerBlob = "$(Invoke-Mirror hash-object -w --no-filters $markerFile)".Trim() }
finally { Remove-Item $markerFile }
if ($rootEntries | Where-Object { "$_".EndsWith("`t.orchestra") }) { throw 'The core itself has a .orchestra file: it belongs to the mirror' }
$rootEntries += "100644 blob $markerBlob`t.orchestra"

foreach ($area in $areaEntries.Keys) {
    if ($areaEntries[$area].Count -eq 0) { continue }
    if ($rootEntries | Where-Object { "$_".EndsWith("`t$area") }) {
        throw "The core itself has a '$area' entry: it must ignore $area/, which belongs to the components"
    }
    $rootEntries += "040000 tree $(New-Tree $areaEntries[$area])`t$area"
}

$tree = New-Tree $rootEntries

if ($parent -and (Invoke-Mirror rev-parse "$parent^{tree}") -eq $tree) {
    Write-Host 'Nothing changed since the last snapshot' -ForegroundColor Yellow
    # A fresh .mirror has the remote branch only: the local one, which a push or a tag names, starts there
    Invoke-Mirror update-ref "refs/heads/$Branch" $parent | Out-Null
}
else {
    if (-not $Message) { $Message = "[MIRROR] Orchestra snapshot $(Get-Date -Format 'yyyy-MM-dd HH:mm')" }
    $body = "$Message`n`nComponents:`n$($lines -join "`n")`n"

    $messageFile = Join-Path $mirror 'SNAPSHOT_MSG'
    [System.IO.File]::WriteAllText($messageFile, $body, [System.Text.UTF8Encoding]::new($false))
    try {
        $arguments = @('commit-tree', $tree, '-F', $messageFile)
        if ($parent) { $arguments += @('-p', $parent) }
        $commit = Invoke-Mirror @arguments
    }
    finally { Remove-Item $messageFile }

    Invoke-Mirror update-ref "refs/heads/$Branch" $commit | Out-Null
    Write-Host "Snapshot $($commit.Substring(0, 10)) on $Branch of the mirror:" -ForegroundColor Green
    $lines | ForEach-Object { Write-Host "  $_" }
}

if ($Tag) {
    Invoke-Mirror tag -a $Tag -m "Orchestra $Tag" "refs/heads/$Branch" | Out-Null
    Write-Host "Tagged $Tag"
}

$refs = @("refs/heads/${Branch}:refs/heads/$Branch")
if ($Tag) { $refs += "refs/tags/${Tag}:refs/tags/$Tag" }

if ($Push) {
    # --quiet: the progress of a push goes to stderr, which Windows PowerShell would take for an error
    Invoke-Mirror push --quiet origin @refs
    Write-Host "Pushed to $Remote" -ForegroundColor Green
}
else {
    Write-Host "Not pushed. To publish it:"
    Write-Host "  git --git-dir `"$mirror`" push origin $($refs -join ' ')"
}
