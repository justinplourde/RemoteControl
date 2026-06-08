# Repository Layout

Last updated: June 7, 2026

## Direction

The outer repository folder is `RemoteControl`. The modern product/solution is currently
`MasterSplinter.sln` at the repository root.

This is a normal .NET repository layout:

```text
RemoteControl/
  MasterSplinter.sln
  docs/
  src/
  tests/
  legacy/
```

The root solution should stay at the root. It gives one obvious command for the main
quality gate:

```powershell
dotnet test .\MasterSplinter.sln
```

## Modern Code

Modern product code belongs under `src`:

- `src/MasterSplinter.Common`
- `src/MasterSplinter.Client.Core`
- `src/MasterSplinter.Client.Host`
- `src/MasterSplinter.Cli`
- `src/MasterSplinter.Operator.WinForms`
- `src/MasterSplinter.Server.Core`
- `src/MasterSplinter.Server.Host`

Future product surfaces should also be added under `src`, for example:

- `src/MasterSplinter.WebApi`
- `src/MasterSplinter.Cli`
- `src/MasterSplinter.Service`
- additional desktop/web surfaces as parity work requires

Tests should stay under `tests` with matching names.

## Legacy Source

The imported archived Quasar code lives under:

```text
legacy/Quasar/
```

That folder is a temporary parity reference. It should not become the long-term product
home. As modern `MasterSplinter.*` projects reach parity, behavior should move into
tested modern libraries under `src`. Once the modern implementation has enough parity,
`legacy/Quasar` can be removed.

## Naming

- `RemoteControl`: outer repository/workspace folder.
- `MasterSplinter`: modern product, solution, project, and namespace prefix.
- `legacy/Quasar`: archived/imported source used only for comparison and extraction.

This lets the repository name describe the broader workspace while the code keeps a
stable product identity during modernization.
