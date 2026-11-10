# modules

The modules of this installation, one folder each. A folder with a `<Id>.stellar.module` file is a module: it is found
automatically by `stellar modules sync`.

```bash
dotnet stellar module new Managed Stellar.Sample --level HighLevel
dotnet stellar modules sync
dotnet stellar modules build
```

A module that lives elsewhere, e.g. a repository being worked on:

```bash
dotnet stellar modules add F:/project/CS/Stellar.Sample
```

See `Docs/Architecture/Modules.md` at the root of the installation.
