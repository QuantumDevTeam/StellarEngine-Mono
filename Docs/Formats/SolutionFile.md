# `.stellar.solution`

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

What a solution of Stellar projects says about itself, named `{Solution}.stellar.solution`, next to its `.sln`. The JSON
schema is `Shared/Schemas/stellar.solution.schema.json`. For now it says how `stellar run` runs the solution.

```json
{
  "Engine": "Packages",
  "Run": {
    "Default": "Game",
    "Profiles": {
      "Game": {
        "Project": "Game/Game.csproj"
      },
      "Network": {
        "Projects": ["Server", "Client"],
        "Configuration": "Release",
        "Arguments": ["--port", "7777"],
        "Environment": { "STELLAR_LOG": "verbose" },
        "Properties": { "StellarNativeWrapper": "Managed" }
      }
    }
  }
}
```

## The layout of a solution

`stellar project new Game Demo` makes:

```text
Demo/
├── Demo.sln                  the projects, and the engine in solution folders
├── Demo.stellar.solution     this file
├── Directory.Build.props     reads "Engine" of this file for MSBuild
├── global.json               the version of Stellar.Sdk
├── NuGet.config              the local feed of the installation
└── Demo/                     the project
    ├── Demo.csproj
    ├── Demo.stellar.project
    └── EntryPoint.cs
```

The projects take the engine from its packages: the engine is a dependency, and the `.sln` lists your projects only.
`--flat` makes the layout without a solution: the project in the directory itself.

## `Engine`

`Packages`, the default, or `Sources`: the projects build the engine and its modules from their sources in Debug, to
work on the engine itself. MSBuild reads no JSON, so the `Directory.Build.props` of the solution looks for the word in
this file and sets `StellarEngine`. After switching, `stellar solution sync` writes the `.sln` again: an IDE loads a
referenced project only from the solution, so with `Sources` the projects of the core (Kernel, Core, Runtime, the
wrapper) go to the solution folder `Engine` and the modules of the installation to `Modules`, built by Debug only.

`stellar solution sync` also adds a project you made in the directory of the solution, or a module you started to
depend on. It writes the `.sln` from scratch: solution folders made by hand in the IDE are not kept.

## `Run`

| Key | Meaning |
|---|---|
| `Default` | What runs without a name: a profile, or an executable project |
| `Profiles` | Ways to run, by name |

A profile:

| Key | Meaning |
|---|---|
| `Project` | The project to run: its `.csproj` relative to the solution, or its name |
| `Projects` | Several projects to run at once, each in a process of its own: a server and its clients |
| `Configuration` | `Debug` when omitted |
| `Arguments` | Arguments of the application; those after `--` on the command line follow them |
| `Environment` | Environment variables of the processes |
| `Properties` | MSBuild properties of the build, such as `StellarNativeWrapper` |

## `stellar run`

```text
stellar run [TARGET] [-c <configuration>] [--no-build] [--dry-run] [-- <arguments>]
```

It works from anywhere in the solution. `TARGET` is a profile first, then an executable project (a project with
`StellarSdkUsage` `ExecutableModule`). Without it: `Run.Default`, the only profile, or the only executable project.
Without a `.stellar.solution`, the project of the directory runs.

One project runs with `dotnet run`, which builds it. Several are built first, one after another, so that two builds never
write the same engine project at once; then they run at once, and `stellar run` waits for all of them. Each application
is started by its own launcher, with the modules of its runtime configuration in one engine, as when it is started by
hand.
