# Copilot Instructions

## Overview

We are implementing `TwoRivers.Berelain` as a standalone open-source Orchard Core module distributed as a NuGet package. The module provides Font Awesome settings, icon curation, icon field editing, diagnostics, and runtime rendering for Orchard Core applications.

## Greenfield Approach

This is a greenfield project. We have the freedom to design the content types, editor experience, and implementation approach in whatever way we think is best.

- Follow Orchard Core conventions and best practices.
- Readable, maintainable code is a requirement, not a stretch goal. Prefer explicit names and small methods over clever or compact implementations.
- Breaking changes are of no concern in v1.0.0. The source code has not yet been published, nor has the NuGet package.
- Getting it right and maintainable is the priority. There is no deadline. Minimizing effort is not important.

When making implementation decisions, use Orchard Core native patterns and documented best practices first. Prefer solutions that reduce long-term technical debt within Orchard conventions, even if they require more upfront work. If a proposed approach is not clearly Orchard-native, explicitly flag it and justify why it is necessary.

### Orchard-First Decision Rules

- **Orchard-First Rule**: For content rendering, composition, and editor experience, prioritize Orchard Core patterns (DisplayDrivers, Shapes, ViewModels, services) over generic ASP.NET patterns unless Orchard documentation explicitly supports both.
- **Architecture Uncertainty Rule**: Before implementing a structural pattern (for example code-behind, routing model, content modeling strategy), verify Orchard support. If support is unclear, stop and present the Orchard-native option first.
- **Razor View Scope Rule**: Keep `.cshtml` files presentation-focused. Move query/business/transformation logic into C# services, DisplayDrivers, and ViewModels. If logic in a view exceeds basic formatting and display conditionals, refactor or create a follow-up issue.

## Documentation and References

- [Repository README](../README.md): development, testing, and NuGet packaging guidance.
- [Module README](../src/TwoRivers.Berelain/README.md): usage, configuration, and runtime behavior.
- [Implementation Plan](../docs/PLAN.md): remediation and implementation plan.
- [Development and Testing Infrastructure](../docs/DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md): host, tests, CI, and tooling guidance.
- [DISCUSSION document](../DISCUSSION.md): untracked working discussion only. Migrate lasting decisions into tracked documentation before implementation.

---

## Architecture Principles

- Keep the module standalone and free of CPCA-specific runtime assumptions.
- Treat Font Awesome assets as host-provided external dependencies under `wwwroot/lib/fontawesome/`.
- Do not distribute licensed Font Awesome assets in the NuGet package.
- Use canonical icon keys only: `family/style/name`.
- Prefer Orchard-native patterns for content fields, display drivers, settings, shapes, and resource management.
- Keep Razor views presentation-focused. Business logic belongs in services, drivers, handlers, and view models.
- Use structured editor controls rather than free-form text or HTML whenever the option set is constrained.

---

## Development Environment

The development environment is Windows 11. Use PowerShell commands in the terminal — POSIX-style commands will not work as expected.

For build, test, asset restore, and packaging guidance, see [README.md](../README.md) and [DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md](../docs/DEVELOPMENT_AND_TESTING_INFRASTRUCTURE.md).
