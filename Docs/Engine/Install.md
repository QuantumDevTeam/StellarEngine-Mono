# Installing StellarEngine

The engine ships as sources and is built on the machine that uses it. One command checks the toolchain, clones the
core, builds it, installs it and registers it for the user:

```powershell
irm https://quantumdevteam.github.io/StellarEngine-Core/install.ps1 | iex
```

## What it needs

| Tool | Required | Get it |
|---|---|---|
| Windows | yes | the native core builds with Visual Studio |
| Git | yes | `winget install --id Git.Git -e` |
| .NET SDK 9.0.300 or later | yes | `winget install --id Microsoft.DotNet.SDK.9 -e` |
| Visual Studio with *Desktop development with C++* | yes | the Visual Studio Installer |
| C++/CLI support of Visual Studio | no | without it the engine uses the P/Invoke wrapper of the native core, which works the same |

The installer checks all of it first and prints the command for what is missing. It installs nothing into the system
by itself.

## Options

Piped into `iex`, a script gets no parameters, so every option is also an environment variable:

| Parameter | Variable | Default |
|---|---|---|
| `-Path` | `STELLAR_INSTALL_PATH` | `~/StellarEngine` |
| `-Ref` | `STELLAR_INSTALL_REF` | `master`; a tag installs a release |
| `-Modules` | `STELLAR_INSTALL_MODULES` | none; names in the organization of the core, URLs or local paths, comma separated |
| `-Configuration` | `STELLAR_INSTALL_CONFIGURATION` | `Release` |
| `-Repository` | `STELLAR_INSTALL_REPOSITORY` | StellarEngine-Core of QuantumDevTeam |
| `-CheckOnly` | | only checks the toolchain |

With parameters:

```powershell
& ([scriptblock]::Create((irm https://quantumdevteam.github.io/StellarEngine-Core/install.ps1))) -Path D:\StellarEngine -Modules Stellar.FileSystem
```

Running it again over the same folder updates the core and installs it again.

## After the install

Open a new terminal, so that the `stellar` command is on the `PATH`:

```powershell
stellar info
stellar modules new Managed Stellar.Sample
powershell -File <root>\scripts\solution.ps1
```

`stellar info` shows the installation and the registry of the user (`~/.stellar`). `scripts/solution.ps1` writes
`StellarEngine.sln`, the core with every module and tool, for the IDE. See [the layout](Repository.md).

## Removing it

```powershell
powershell -File <root>\scripts\build.ps1 -Uninstall
```

The checkout becomes a plain checkout of the core again: the registration, the `stellar` command, the packages of
the installation in the NuGet cache and the build output go. Modules, tools and games in their folders stay.

## By hand

```powershell
git clone https://github.com/QuantumDevTeam/StellarEngine-Core.git StellarEngine
powershell -File StellarEngine\scripts\build.ps1 -Configuration Release -Install
```
