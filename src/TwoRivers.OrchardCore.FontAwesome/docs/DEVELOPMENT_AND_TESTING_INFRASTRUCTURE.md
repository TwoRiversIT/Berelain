# Development and Testing Infrastructure Proposal

## Purpose

This document proposes the long-term development and testing infrastructure for `TwoRivers.Berelain`.

The goal is not to optimize for a one-time rescue effort. The goal is to establish a development system that supports:

- sustained module development over multiple releases
- safe refactoring of Orchard-specific behavior
- fast feedback for parsing, curation, rendering, and diagnostics logic
- repeatable local development without depending on an external shared host project
- clear separation between licensed Font Awesome assets and source-controlled module code

## Guiding Principles

1. The module remains a standalone Orchard Core module and must not depend on CPCA-specific hosts, themes, or recipes.
2. Licensed Font Awesome assets are never committed to source control.
3. The development host must be easy to reset, but reset must become an explicit scripted workflow instead of tribal knowledge.
4. Automated testing must be layered. Unit, integration, and optional UI tests solve different problems and should not be conflated.
5. Test suites must use `xunit.v3`, `Moq`, and `FluentAssertions`.
6. Development and test workflows should prefer the existing reproducible npm-based Font Awesome asset restore mechanism over hand-authored synthetic vendor fixtures.
7. Additional libraries and tooling must be explicitly approved before adoption.

## Proposed Repository Structure

```text
TROCFA.sln
Directory.Build.props
Directory.Packages.props                  # proposed
build/
  Invoke-Build.ps1                       # proposed
  Reset-DevelopmentHost.ps1              # proposed
  Sync-FontAwesomeAssets.ps1             # proposed
  Test.ps1                               # proposed
hosts/
  TwoRivers.Berelain.DevelopmentHost/   # proposed
tests/
  TwoRivers.Berelain.TestCommon/        # proposed
  TwoRivers.Berelain.UnitTests/         # proposed
  TwoRivers.Berelain.IntegrationTests/  # proposed
  TwoRivers.Berelain.UiTests/           # proposed
src/
  TwoRivers.OrchardCore.FontAwesome/
```

## Solution Layout

The solution should expand from a single module project to a small but explicit working set.

### 1. Module project

- `src/TwoRivers.OrchardCore.FontAwesome`
- Remains the only packable project in the solution.

### 2. Development host project

- `hosts/TwoRivers.Berelain.DevelopmentHost`
- Purpose:
  - local manual development
  - Orchard admin verification
  - recipe execution
  - debugger attachment
  - manual browser and Playwright sessions
- This host should be dedicated to this module, not shared across unrelated modules.

### 3. Test common project

- `tests/TwoRivers.Berelain.TestCommon`
- Purpose:
  - shared fixture builders
  - sample metadata/css/svg loaders
  - shared temp-directory helpers
  - Orchard test constants
  - recipe helpers
- This prevents unit and integration test projects from duplicating support code.

### 4. Unit test project

- `tests/TwoRivers.Berelain.UnitTests`
- Purpose:
  - fast tests for pure logic
  - no web server
  - no real Orchard boot unless unavoidable

### 5. Integration test project

- `tests/TwoRivers.Berelain.IntegrationTests`
- Purpose:
  - host-level verification
  - recipe/setup verification
  - controller endpoints
  - content definition wiring
  - resource generation and file outputs

### 6. Optional UI test project

- `tests/TwoRivers.Berelain.UiTests`
- Purpose:
  - end-to-end admin picker behavior
  - keyboard interaction
  - modal behavior
  - rendered frontend smoke tests
- This should be added only after tooling approval.

## Development Host Proposal

## Host Responsibilities

The dedicated host should provide a deterministic environment for local development without becoming part of the module package.

### Required characteristics

1. References the local module project directly.
2. Uses a dedicated Orchard recipe for module setup.
3. Uses SQLite by default for low-friction local development.
4. Uses a known admin account and password stored in development-only configuration.
5. Supports a scripted reset operation.
6. Reads host-provided Font Awesome assets from `wwwroot/lib/fontawesome/`.

## Host Configuration Strategy

### Recommended files

- `appsettings.json`
- `appsettings.Development.json`
- `Recipes/FontAwesome.Development.recipe.json`
- `wwwroot/lib/fontawesome/` as the effective asset target

### Licensed asset handling

Licensed assets should not live in tracked source. Instead:

1. The module's existing `package.json` provides the reproducible development-time mechanism for restoring Font Awesome packages through npm.
2. Font Awesome package access is authenticated through environment variables that supply the Font Awesome license credentials.
3. npm-restored assets are used as the development-time source for metadata, CSS, JS, webfonts, and SVGs.
4. `build/Sync-FontAwesomeAssets.ps1` copies the required files into the development host's `wwwroot/lib/fontawesome/`.
5. The host and module always behave as if the host application provided those assets normally.

This keeps the runtime contract honest while making development practical.

## NPM-Based Asset Provisioning

The repository already contains an important piece of long-term infrastructure: `src/TwoRivers.OrchardCore.FontAwesome/package.json` is used to restore the full Font Awesome asset set for development and testing in a reproducible way.

### Required behavior

1. Document the npm restore flow as the standard development-time asset acquisition mechanism.
2. Treat npm restore as a development and test prerequisite for any workflow that exercises real Font Awesome metadata, CSS, SVGs, or webfonts.
3. Keep the runtime contract unchanged: the module still consumes assets from the host application's `wwwroot/lib/fontawesome/`.
4. Use sync or copy scripts to move the required files from the npm-restored source into the development host's web root.

### Environment variables

The Font Awesome license key and any related npm authentication settings must be provided through environment variables.

### Documentation requirement

The final development documentation should include:

1. the required environment variable names
2. where those variables must be set for local development
3. how they are provided in CI through secrets
4. what `npm install` or equivalent restore command the developer runs
5. how restored assets are synchronized into the development host

### Package classification

The current Font Awesome package references live under `dependencies` in `src/TwoRivers.OrchardCore.FontAwesome/package.json`.

To reduce ambiguity, they should be moved to `devDependencies` unless a later documented workflow requires them at runtime for non-development scenarios.

## Reset Workflow

The old manual `App_Data` deletion workflow should become a script.

### Proposed reset script

- `build/Reset-DevelopmentHost.ps1`

### Responsibilities

1. Stop any running development host process if needed.
2. Delete host `App_Data`.
3. Delete test output folders generated by previous integration or UI runs.
4. Optionally preserve or refresh untracked Font Awesome assets.
5. Restart the host when requested.

### Recommendation

Keep the setup recipe auto-run capability only for the dedicated development host and integration test host. Do not design the module itself around this convenience.

## Shared Build Configuration

## Directory.Build.props

Retain root-wide shared .NET settings, but extend them for a multi-project solution.

### Recommended shared properties

- `TargetFramework` where appropriate
- `Nullable` enabled
- `ImplicitUsings` enabled
- `TreatWarningsAsErrors` for CI builds
- deterministic builds enabled for CI
- analyzer execution enabled using built-in SDK analyzers

## Directory.Packages.props

Add central package management.

### Why

1. Keeps OrchardCore package versions aligned.
2. Keeps test stack versions aligned across all test projects.
3. Makes upgrades explicit and reviewable.
4. Prevents drift between test projects over time.

### Recommended centrally managed packages

- OrchardCore packages already used by the module
- `xunit.v3`
- `Moq`
- `FluentAssertions`
- any approved test-only infrastructure packages

### Approved supporting packages and tooling

The following are now approved for infrastructure implementation:

- `Microsoft.NET.Test.Sdk`
- `Microsoft.AspNetCore.Mvc.Testing` or equivalent ASP.NET Core host-testing package
- `coverlet.collector`
- Playwright tooling for the UI test project

## Build Scripts

Add a minimal scripted build surface under `build/`.

### Proposed scripts

1. `Invoke-Build.ps1`
   - restore
   - build
   - pack module when requested
2. `Test.ps1`
   - run unit tests
   - run integration tests
   - optionally run UI tests
3. `Reset-DevelopmentHost.ps1`
4. `Sync-FontAwesomeAssets.ps1`

These should wrap common workflows without hiding what they do.

### Recommended script responsibilities

`Sync-FontAwesomeAssets.ps1` should:

1. validate required environment variables are present
2. verify npm restore has completed successfully
3. copy the required asset directories into the development host's `wwwroot/lib/fontawesome/`
4. avoid copying unnecessary files when a narrower sync is sufficient
5. support CI and local use without manual path editing

## Test Strategy

The module needs a formal testing pyramid.

## Layer 1. Unit Tests

### Purpose

Protect pure logic and fast feedback paths.

### Framework and libraries

- `xunit.v3`
- `Moq`
- `FluentAssertions`

### Scope

Unit tests should cover:

1. canonical key parsing and normalization
2. allowlist filtering by family and style
3. metadata normalization from sample input
4. curated manifest creation
5. runtime allowlist generation
6. CSS selection or generation helpers
7. asset resolution rules
8. render service logic that does not require a full server
9. diagnostics aggregation logic over prepared content models

### Rules

1. No real HTTP server.
2. No dependency on real Orchard startup unless a class cannot be isolated.
3. For pure parsing and transformation tests that need real asset structure, source inputs from the npm-restored Font Awesome packages rather than hand-authored synthetic vendor fixtures.
4. For fully isolated logic tests, inline builders and mocked objects are still appropriate when no real asset semantics are required.

## Layer 2. Integration Tests

### Purpose

Verify Orchard wiring and module behavior inside a running host.

### Framework and libraries

- `xunit.v3`
- `Moq` where a dependency must be substituted inside the host
- `FluentAssertions`

### Recommended host model

Use a dedicated test host bootstrapped specifically for integration tests.

Where integration coverage depends on real Font Awesome asset behavior, the test host should use assets synchronized from the npm-restored packages into a test-specific `wwwroot/lib/fontawesome/` location.

### Scope

Integration tests should cover:

1. module startup and service registration
2. data migration execution
3. settings publication triggering artifact regeneration
4. curated icons endpoint contract
5. diagnostics endpoint behavior
6. field display driver and settings display driver wiring
7. generated artifact file creation
8. missing asset behavior
9. resource registration behavior

### Database strategy

Use SQLite for integration tests.

### Reset strategy

Each integration test class or collection should use an isolated temp content root and temp `App_Data` directory. Tests must not share mutable filesystem state unless they are intentionally exercising a shared-host scenario.

## Layer 3. UI Tests

### Purpose

Catch behavior that unit and integration tests cannot prove, especially in the Orchard admin UI.

### Recommended scope

1. icon picker modal opens correctly
2. curated icons load
3. preview glyph is shown
4. search filters results
5. keyboard navigation works
6. apply and clear actions persist correctly
7. settings UI behaves correctly

### Recommendation

UI tests should exist, but they should start as a targeted smoke suite rather than a large end-to-end suite.

### Tooling note

Playwright is approved and should be used for the optional UI smoke suite.

## Test Assets Strategy

Because the module must not distribute licensed Font Awesome assets, the automated test suite must use npm-restored development assets or locally constructed in-memory data, depending on what the specific test is trying to prove.

## Preferred asset sources

1. Font Awesome assets restored through the existing npm workflow
2. copied subsets of those restored assets in temporary test directories
3. in-memory test objects created by fixture builders when real vendor files are unnecessary

## Disallowed test assets

1. committed Pro or Pro+ distributions
2. committed vendor zips
3. checked-in vendor-derived fixtures that bypass the documented npm and environment-variable workflow

## Orchard-Specific Testing Conventions

## Recipe strategy

Use recipes deliberately instead of as a catch-all bootstrap mechanism.

### Recommended recipe split

1. `FontAwesome.Development.recipe.json`
   - local manual development convenience
2. `FontAwesome.Integration.recipe.json`
   - smallest recipe needed for integration tests
3. optional scenario recipes for focused behavior under test

### Rule

Recipes should define module-owned Orchard setup only. Do not embed unrelated site content or assumptions into the module's development host.

## Fixture strategy

Use xUnit v3 fixtures to control expensive setup.

### Recommended fixture types

1. `CollectionFixture` for host bootstrapping that can be shared safely
2. class fixtures for temp filesystem roots and seeded Orchard instances
3. helper builders in `TestCommon` for content items and settings payloads

## CI Proposal

The project should have automated CI from the beginning of the rebuild, even if UI tests are introduced later.

## Minimum CI pipeline

1. restore
2. build solution
3. run unit tests
4. run integration tests
5. pack module in Release configuration

## Recommended CI shape

### Pull request validation

- build
- unit tests
- integration tests

### Main branch validation

- build
- unit tests
- integration tests
- pack
- publish artifacts

### Optional scheduled pipeline

- UI smoke tests
- longer-running diagnostics and recipe tests

## CI and Secret Management

Any CI workflow that restores Font Awesome packages or runs asset-backed tests must provide the required Font Awesome npm authentication values through CI secrets.

### Required CI behavior

1. inject the required environment variables securely
2. run npm restore before asset-backed test phases
3. sync assets into the dedicated host or test host before integration and UI tests
4. fail fast with a clear error when required secrets are not available for workflows that need real assets

## Cross-platform recommendation

Even though day-to-day development is on Windows, the module code should aim to remain portable. Unit and integration tests should be designed to run on both Windows and Linux in CI where feasible.

## Coverage Expectations

Coverage targets should guide behavior, not become vanity metrics.

### Recommended targets

- high coverage for parsing, normalization, curation, and generation logic
- moderate coverage for Orchard drivers and controllers
- smoke coverage for UI behavior

### Recommendation

Track line coverage for unit and integration tests once coverage tooling is approved, but do not block initial infrastructure creation on perfect coverage instrumentation.

## Approved Tooling

The following supporting libraries and tooling are approved for implementation:

1. `Microsoft.NET.Test.Sdk`
2. `Microsoft.AspNetCore.Mvc.Testing` or equivalent ASP.NET Core host test package
3. `coverlet.collector`
4. Playwright tooling for Orchard admin and smoke-test automation

The proposal still does not assume snapshot testing libraries, browser visual diff tooling, or third-party analyzer packages.

## Recommended Implementation Order

1. Add `Directory.Packages.props`.
2. Add the dedicated development host.
3. Add `TestCommon`.
4. Add the unit test project using `xunit.v3`, `Moq`, and `FluentAssertions`.
5. Add the integration test project and its dedicated recipe.
6. Add build and reset scripts.
7. Add CI workflow.
8. Add optional UI smoke tests after tooling approval.

## Definition of Done for Infrastructure

The infrastructure is ready for ongoing development when:

1. a developer can clone the repo, sync private Font Awesome assets, reset the host, and start coding without undocumented manual steps
2. unit tests run locally in seconds
3. integration tests run locally and in CI with deterministic results
4. no committed test or host asset violates Font Awesome licensing constraints
5. the dedicated host can reproduce module setup and editor workflows consistently
6. build, test, and reset workflows are scriptable and documented

## Recommended Outcome

The best long-term structure is:

- one packable module project
- one dedicated development host
- one shared test utility project
- one unit test project
- one integration test project
- one optional UI test project
- scripted local workflows
- CI that validates build plus automated tests on every meaningful change

This gives the module a sustainable foundation for v1 salvage, v1 hardening, and future feature work without re-architecting the development environment again later.