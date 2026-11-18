# modules

The modules of this installation, one folder each. A folder with a `<Id>.stellar.module` file is a module: it is found
automatically by `stellar modules sync`.

```bash
stellar module new Managed Stellar.Sample --level HighLevel
stellar modules sync
stellar modules build
```

A module that lives elsewhere, e.g. a repository being worked on:

```bash
stellar modules add ../Stellar.Sample
```

See `Docs/Engine/Modules.md` at the root of the installation.
