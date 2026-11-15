<#
.SYNOPSIS
    Generates the DocFX project of the orchestra site: the core and every module of the tree in one site.

.DESCRIPTION
    The mirror (StellarEngine-Mono) holds the core at its root and every component in modules/<Module>/ and
    tools/<Tool>/. Its site gathers them:

        /                    the home of StellarEngine: getting started, what the orchestra is made of
        /guide/              the guide of the whole engine: Docs/Engine of the core (installing, architecture,
                             modules, the orchestra, the project), with README, CONTRIBUTING, LICENSE, CLA at /
        /core/               the core as a component: the rest of its Docs/, /core/api/ its API reference
        /modules/<Module>/   the Docs/ of each module (each also has a site of its own)
        /tools/<Tool>/       the same for tools
        /install.ps1         the installer, /schemas/ the JSON schemas

    The project is written to docfx.orchestra.json at the root, its pages to artifacts/orchestra/ (both generated,
    never edited, ignored by git); build it with 'docfx docfx.orchestra.json'. The docs workflow does that when the tree is a mirror: scripts/mirror.ps1
    puts a .orchestra file at the root of every snapshot, with the commits it was made of.

.PARAMETER Output
    The folder of the generated pages (the hub and its navigation). artifacts/orchestra by default.
#>
param(
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Output) { $Output = Join-Path $root 'artifacts/orchestra' }
$Output = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force $Output | Out-Null

# DocFX takes the paths of a project relative to it
function Get-FromProject([string]$Path) {
    $from = [System.Uri]::new($Output.TrimEnd('\', '/') + '/')
    $to = [System.Uri]::new([System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/') + '/')
    return [System.Uri]::UnescapeDataString($from.MakeRelativeUri($to).ToString()).TrimEnd('/')
}

# The project itself lies at the root, above every source: DocFX resolves nested tables of contents wrongly for
# sources above the folder of its project
$project_ = Join-Path $root 'docfx.orchestra.json'
function Get-FromRoot([string]$Path) {
    return [System.IO.Path]::GetFullPath($Path).Substring($root.TrimEnd('\', '/').Length).TrimStart('\', '/').Replace('\', '/')
}

$versions = [System.IO.File]::ReadAllText((Join-Path $root 'Shared/Build/Stellar.Versions.props'))
$orchestra = [regex]::Match($versions, '<StellarOrchesterVersion>([^<]+)<').Groups[1].Value
$site = [regex]::Match($versions, '<StellarSiteUri>([^<]+)<').Groups[1].Value
$host_ = ([System.Uri]$site).GetLeftPart([System.UriPartial]::Authority)

# What the snapshot was made of, if this is one
$snapshot = @{}
$marker = Join-Path $root '.orchestra'
if (Test-Path $marker) {
    foreach ($component in (Get-Content $marker -Raw | ConvertFrom-Json).Components) { $snapshot[$component.Name] = $component }
}

$rootPath = Get-FromProject $root
# Links of the hub name source files, as DocFX resolves them; the site paths follow from the content mapping below
$engineDocs = Get-FromProject (Join-Path $root 'Docs/Engine')
$coreDocs = Get-FromProject (Join-Path $root 'Docs')
$coreApi = Get-FromProject (Join-Path $root 'artifacts/docs/api')
$content = [System.Collections.Generic.List[object]]::new()
$navigation = [System.Collections.Generic.List[string]]::new()
$rows = [System.Collections.Generic.List[string]]::new()

# ---- The guide of the engine ----
# Docs/Engine of the core: the pages about the engine as a whole live there, published here only
$content.Add([ordered]@{ src = 'Docs/Engine'; dest = 'guide'; files = @('**/*.md', 'toc.yml') })
$content.Add([ordered]@{ files = @('README.md', 'CONTRIBUTING.md', 'LICENSE.md', 'CLA.md') })

# ---- The core, a component ----
$content.Add([ordered]@{ src = 'Docs'; dest = 'core'; files = @('**/*.md', 'toc.yml'); exclude = @('Engine/**') })
$content.Add([ordered]@{ src = 'artifacts/docs/api'; dest = 'core/api'; files = @('**/*.yml') })
$coreCommit = if ($snapshot['StellarEngine-Core']) { "``$($snapshot['StellarEngine-Core'].Commit.Substring(0, 10))``" } else { '' }
$coreRepository = if ($snapshot['StellarEngine-Core'] -and $snapshot['StellarEngine-Core'].Repository) { ", [repository]($($snapshot['StellarEngine-Core'].Repository -replace '\.git$', ''))" } else { '' }
$rows.Add("| [StellarEngine-Core]($coreDocs/index.md) | the kernel, the core, the runtime, the SDK and the tools | $coreCommit | [site]($site)$coreRepository |")

# ---- The components ----
$componentNavigation = [System.Collections.Generic.List[string]]::new()
foreach ($area in 'modules', 'tools') {
    $directory = Join-Path $root $area
    if (-not (Test-Path $directory)) { continue }

    foreach ($component in (Get-ChildItem $directory -Directory | Sort-Object Name)) {
        if ($component.Name.StartsWith('_')) { continue }
        # tools/compilers belongs to the installer, not to a component
        if ($area -eq 'tools' -and $component.Name -eq 'compilers') { continue }
        $docs = Join-Path $component.FullName 'Docs'

        $description = ''
        $descriptor = Join-Path $component.FullName "$($component.Name).stellar.module"
        if (Test-Path $descriptor) { $description = (Get-Content $descriptor -Raw | ConvertFrom-Json).Description }

        $commit = if ($snapshot[$component.Name]) { "``$($snapshot[$component.Name].Commit.Substring(0, 10))``" } else { '' }
        $componentDocs = Get-FromProject $docs
        $own = "$host_/$($component.Name)/"
        $repository = if ($snapshot[$component.Name] -and $snapshot[$component.Name].Repository) { ", [repository]($($snapshot[$component.Name].Repository -replace '\.git$', ''))" } else { '' }

        if (Test-Path (Join-Path $docs 'index.md')) {
            $content.Add([ordered]@{ src = Get-FromRoot $docs; dest = "$area/$($component.Name)"; files = @('**/*.md', 'toc.yml') })
            # The README of the component, which its pages link to as ../README.md: same place in the site
            if (Test-Path (Join-Path $component.FullName 'README.md')) {
                $content.Add([ordered]@{ src = Get-FromRoot $component.FullName; dest = "$area/$($component.Name)"; files = @('README.md') })
            }
            $rows.Add("| [$($component.Name)]($componentDocs/index.md) | $description | $commit | [site]($own)$repository |")
            $componentNavigation.Add("    - name: $($component.Name)`n      href: $componentDocs/index.md")
        }
        else {
            $rows.Add("| $($component.Name) | $description | $commit | [site]($own)$repository |")
        }
    }
}

# ---- The hub ----
$installUri = "${site}install.ps1"
$engineLogo = "$rootPath/Shared/Branding/stellar_1x1.svg"
$brandLogo = "$rootPath/Shared/Branding/logo_clean.svg"
$index = @"
<p align="center"><img src="$engineLogo" alt="StellarEngine" width="200"/></p>

# StellarEngine $orchestra

A hypermodular game engine built from the bottom up: a C++ native core, a managed core over it, and everything else as
modules, each in a repository of its own. This is the site of the whole engine: the documentation, the API reference,
and every module and tool of the orchestra, built from StellarEngine-Mono, the mirror of all of them.

## Getting started

1. **Install the engine.** One command checks the toolchain, clones the core, builds and registers it
   ([what it needs and does]($engineDocs/Install.md)):

   ``````powershell
   irm $installUri | iex
   ``````

2. **Check it.** Open a new terminal, so the ``stellar`` command is on the ``PATH``:

   ``````powershell
   stellar info
   ``````

3. **Make a module** in the installation, and the solution of the whole installation for the IDE:

   ``````powershell
   stellar modules new Managed Stellar.MyModule
   powershell -File <root>\scripts\solution.ps1
   ``````

4. **Build and test it:**

   ``````powershell
   stellar modules build Stellar.MyModule --tests
   ``````

5. **Read on:** the [architecture]($engineDocs/Architecture.md), the [design philosophy]($engineDocs/DesignPhilosophy.md),
   [modules]($engineDocs/Modules.md), the [installation and the repositories]($engineDocs/Repository.md),
   and [the core]($coreDocs/index.md) with its [API reference](xref:Stellar.Kernel).

## The orchestra

Every component of this snapshot, with the commit it was taken at, its own site and its repository:

| Component | What | Commit | Links |
|---|---|---|---|
$($rows -join "`n")

---

<img src="$brandLogo" alt="QuantumDevTeam" width="32"/> StellarEngine is made by QuantumDevTeam.
"@
[System.IO.File]::WriteAllText((Join-Path $Output 'index.md'), $index + "`n", [System.Text.UTF8Encoding]::new($false))

$toc = @"
- name: Home
  href: index.md
- name: Install
  href: $engineDocs/Install.md
- name: Guide
  href: $engineDocs/
- name: Core
  href: $coreDocs/
- name: API
  href: $coreApi/
"@
if ($componentNavigation.Count -gt 0) { $toc += "`n- name: Modules`n  items:`n$($componentNavigation -join "`n")" }
[System.IO.File]::WriteAllText((Join-Path $Output 'toc.yml'), $toc + "`n", [System.Text.UTF8Encoding]::new($false))

$content.Insert(0, [ordered]@{ src = Get-FromRoot $Output; files = @('index.md', 'toc.yml') })

$project = [ordered]@{
    metadata = @(
        [ordered]@{
            src        = @([ordered]@{
                    src   = '.'
                    files = @(
                        'Kernel/src/Managed/Stellar.Kernel/Stellar.Kernel.csproj',
                        'Core/src/Managed/Stellar.Core/Stellar.Core.csproj',
                        'Runtime/src/Managed/Stellar.Runtime/Stellar.Runtime.csproj',
                        'SDK/src/Stellar.Tools/Stellar.Tools.csproj')
                })
            dest       = 'artifacts/docs/api'
            properties = [ordered]@{ TargetFramework = 'net9.0'; StellarNativeWrapper = 'Managed' }
            memberLayout = 'separatePages'
            namespaceLayout = 'nested'
            enumSortOrder = 'declaringOrder'
        })
    build    = [ordered]@{
        content        = $content
        resource       = @(
            [ordered]@{ files = @('Shared/Branding/stellar_1x1.svg', 'Shared/Branding/stellar_icon.svg', 'Shared/Branding/logo_clean.svg') },
            [ordered]@{ src = 'Shared/Schemas'; dest = 'schemas'; files = @('*.json') },
            [ordered]@{ src = 'scripts/bootstrap'; files = @('install.ps1') })
        output         = 'artifacts/docs/orchestra-site'
        template       = @('default', 'modern', 'Shared/Docs/Template')
        globalMetadata = [ordered]@{
            _appName        = 'StellarEngine'
            _appTitle       = "StellarEngine $orchestra"
            _appFooter      = 'StellarEngine by QuantumDevTeam'
            _appLogoPath    = 'Shared/Branding/stellar_icon.svg'
            _appFaviconPath = 'Shared/Branding/stellar_icon.svg'
            _enableSearch   = $true
        }
    }
}
[System.IO.File]::WriteAllText($project_, ($project | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))

Write-Host "Orchestra site project: $project_ (core + $($componentNavigation.Count) component site(s))" -ForegroundColor Green
