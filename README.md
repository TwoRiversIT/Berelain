# Two Rivers Orchard Core Font Awesome

This repository contains the source for `TwoRivers.OrchardCore.FontAwesome`, a standalone Orchard Core module for Font Awesome icon management, curation, and rendering.

For module usage and host application setup, see [src/TwoRivers.OrchardCore.FontAwesome/README.md](src/TwoRivers.OrchardCore.FontAwesome/README.md).

## Documentation

- [src/TwoRivers.OrchardCore.FontAwesome/README.md](src/TwoRivers.OrchardCore.FontAwesome/README.md): module usage, installation, configuration, and runtime behavior.
- [src/TwoRivers.OrchardCore.FontAwesome/docs/PLAN.md](src/TwoRivers.OrchardCore.FontAwesome/docs/PLAN.md): implementation and remediation plan.
- [src/TwoRivers.OrchardCore.FontAwesome/docs/DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md](src/TwoRivers.OrchardCore.FontAwesome/docs/DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md): development host, automated testing, CI, and tooling strategy.

## Development

### Prerequisites

- .NET SDK 8.x
- Node.js and npm
- Access to the Font Awesome npm registry for development-time asset restore
- PowerShell on Windows for the scripted workflows used in this repository

### Development-Time Font Awesome Assets

This repository does not commit licensed Font Awesome assets.

Development and automated tests use the npm-based restore workflow defined in [src/TwoRivers.OrchardCore.FontAwesome/package.json](src/TwoRivers.OrchardCore.FontAwesome/package.json). Font Awesome authentication is supplied through environment variables.

The restored assets are for development and test workflows only. They are not part of the NuGet package.

### Build

Build the current solution from the repository root:

```powershell
dotnet build .\TROCFA.sln
```

### Testing

The long-term testing infrastructure is defined in [src/TwoRivers.OrchardCore.FontAwesome/docs/DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md](src/TwoRivers.OrchardCore.FontAwesome/docs/DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md).

Current repository direction:

- unit tests will use `xunit.v3`, `Moq`, and `FluentAssertions`
- integration tests will run against a dedicated Orchard Core development host
- UI smoke tests will use Playwright

### Build Performance Guardrail

The module project excludes heavy Font Awesome directories from Orchard module-asset embedding so local development assets do not balloon build times.

- Configuration lives in [src/TwoRivers.OrchardCore.FontAwesome/TwoRivers.OrchardCore.FontAwesome.csproj](src/TwoRivers.OrchardCore.FontAwesome/TwoRivers.OrchardCore.FontAwesome.csproj).
- These exclusions are a development-time safeguard only. They do not change the module's host asset contract.

## NuGet Packaging

NuGet packaging is the deployment boundary for this repository.

- The NuGet package must not contain licensed Font Awesome assets.
- Consuming applications must provide Font Awesome assets under `wwwroot/lib/fontawesome/`.
- The package should contain only the Orchard Core module, its code, its views, its scripts, and its documentation.

Package the module from the repository root with:

```powershell
dotnet pack .\src\TwoRivers.OrchardCore.FontAwesome\TwoRivers.OrchardCore.FontAwesome.csproj -c Release
```

## Repository Policies

- Keep the module standalone and free of CPCA-specific runtime assumptions.
- Do not commit licensed Font Awesome payloads.
- Keep `DISCUSSION.md` as an untracked working document only. Preserve decisions in tracked documentation before packaging or release.
