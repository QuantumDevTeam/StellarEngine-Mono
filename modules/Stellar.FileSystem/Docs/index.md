# Stellar.FileSystem

File system of the engine

A `CoreModule` module of [StellarEngine](https://quantumdevteam.github.io/StellarEngine-Mono/). Its site is https://quantumdevteam.github.io/Stellar.FileSystem/, built from the Markdown
files of `Docs/` with DocFX; the whole engine, with every module, is on the site of the orchestra.

## Using it

Add `Stellar.FileSystem` to `Dependencies` of your module or project descriptor, then:

```powershell
stellar modules sync
```

```csharp
using var files = VirtualFileSystem.CreateDefault();
files.Mount(new DomainMount("assets", DomainScheme.Directory, assetsDirectory));
files.Mount(new DomainMount("mods", DomainScheme.Directory, modsDirectory));
files.Mount(new DomainMount("game", DomainScheme.Overlay, "mods,assets"));

var level = files.ReadAllText(FilePath.Parse("game://levels/1.json"));
```

The code names domains, never places of the machine: those are given once, when the runtime mounts the domains.

## Drivers

| Scheme | Source | What it can do |
|---|---|---|
| `dir` | A full path of a directory | Everything; `readonly=true` reads only. Links out of the directory are refused (`links=follow` allows them); atomic writes go beside the file as `*.stellar-tmp` |
| `asm` | The name of an assembly | Reads embedded resources at their logical names, without a copy; `prefix=` takes a part of them |
| `mem` | Not used | Everything, in memory: a write becomes the file when its stream closes |
| `overlay` | Domains, first wins: `mods,dlc,base` | Reads the first layer that has a file; `write=<layer>` sends writes there, copying a file of a lower layer up first |

A driver of another scheme (an archive, a network, a world of a game) implements `IFileDriver` and is registered
with `AddDriver` (the catalog `Drivers`); the contracts are in `Stellar.Kernel.FileSystem`.

## Working on it

```powershell
stellar modules build Stellar.FileSystem --tests
```

See [the README](../README.md) for the layout of the module.
