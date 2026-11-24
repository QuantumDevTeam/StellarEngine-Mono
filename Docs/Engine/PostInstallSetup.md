# Setting up the machine after the install

- [README.md](../../README.md)

What to know once the installation stands: how git behaves in a tree of nested repositories.

## Git in a tree of repositories

An installation is several repositories in one folder ([Repository](Repository.md)):

| Folder | Repository |
|---|---|
| `<root>/` | StellarEngine-Core |
| `<root>/modules/<Module>/` | one repository per module |
| `<root>/tools/<Tool>/` | one repository per tool |

Git has no idea of this tree. A command acts on the nearest `.git` found going up from the folder it runs in, and the
core ignores `modules/` and `tools/`, so:

- `git status` in `<root>` never mentions a module, and in a module never mentions the core;
- inside a module the folder is its own repository: its own history, its own branches, its own `origin`;
- `modules/` and `tools/` hold files of their own (`Directory.Build.props`, `README.md`, ...) that `stellar install`
  wrote: they belong to no repository.

Standing in the folder of a module is enough, `git status` there is the status of that module. `-C` is for running a
command in another folder without leaving yours:

```powershell
git -C modules/Stellar.FileSystem status
```

Three checks that tell where you are:

```powershell
git rev-parse --show-toplevel      # the root of the repository the command acts on
git status --short --branch        # the branch, whether it follows a remote, what changed
ls -Force                          # .git is a hidden folder: without -Force PowerShell does not list it
```

If `git rev-parse --show-toplevel` fails with "not a git repository", the folder is not inside any repository: a module
that has not been through `git init` yet, or a copy of an installation made without the `.git` folders.

### A new module needs its repository

`stellar modules new` creates the files only. A module has a repository of its own once it went through `git init`,
`git remote add origin <url>` and a first push of its branch; until then it has no history anyone else can see and the
mirror (`scripts/mirror.ps1`) has nothing to take from it.

### "Dubious ownership"

Git refuses a repository whose folder belongs to another account than the one running it, which happens to folders
created from an elevated shell or copied from another machine. The refusal names the exact command to run:

```powershell
git config --global --add safe.directory <path of that folder>
```

Do it only for folders you trust; there is no need for `*`.
