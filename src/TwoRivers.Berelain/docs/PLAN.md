# Font Awesome Remediation Plan

## Status and Authority

- [../src/TwoRivers.Berelain/README.md](../src/TwoRivers.Berelain/README.md) is the authoritative definition of intended module behavior and delivery scope.
- `README.md` is the authoritative repository-level document for development, testing, and NuGet packaging.
- `DISCUSSION.md` is a working discussion document and must not be treated as implementation truth until decisions are copied into tracked documentation.

## Purpose

This document defines the plan to bring `TwoRivers.Berelain` from its current partially working state to a production-ready, standalone Orchard Core module that:

- ships without Font Awesome licensed assets
- consumes host-provided Font Awesome assets from a stable, documented location
- stores icon identity only as canonical keys in the format `family/style/name`
- provides an Orchard-native editor experience for icon selection
- generates reliable runtime artifacts from authoritative metadata
- supports curated rendering with a clean path to SVG-based rendering

## Confirmed Constraints

- The module must remain independent of `CPCA.Theme`, ASSAN, and any CPCA-specific branding or content assumptions.
- Font Awesome assets must not be distributed with the module in any form.
- The host application is responsible for provisioning Font Awesome assets under `wwwroot/lib/fontawesome/`.
- Free-form icon specification is out of scope for v1.0.0.
- Any ambiguity between existing code and the authoritative README must be resolved in favor of the README.

## Current Implementation Problems

### 1. Asset discovery is inconsistent

The current code probes multiple version-specific and development-only locations, including `fontawesome-pro-plus-7.2.0-web` and `node_modules`-adjacent assumptions. This conflicts with the authoritative requirement that the host provides assets under `wwwroot/lib/fontawesome/`.

### 2. Canonical key enforcement is incomplete

The current implementation still contains free-form and legacy-oriented logic in diagnostics and key parsing. That conflicts with the greenfield rule that icon identity must not be free-form.

### 3. Picker contract is internally broken

The picker JavaScript depends on `unicode` for preview glyph rendering, but the curated icons endpoint omits `unicode` from its JSON payload.

### 4. CSS generation is not production-safe

The current subset CSS generation depends on fragile string and line-based extraction from minified CSS, uses stale `FontAwesome6*` font-family names, and writes output to a location and filename that do not match the historical strategy documents.

### 5. Rendering modes are not yet coherently defined

The module currently mixes webfont rendering, curated CSS generation, and an optional SVG sprites feature, but the exact v1 rendering contract is not documented clearly enough for implementation and testing.

### 6. Documentation and implementation drift exists

Historical documents describe completed or planned behaviors that do not align cleanly with the current code. The implementation plan must therefore include documentation cleanup as a tracked deliverable, not an afterthought.

## Delivery Strategy

Implementation should proceed in ordered groups. Each group ends with validation and documentation updates before moving to the next.

## Group 1. Establish the authoritative runtime contract

### Group 1 Goal

Remove ambiguity around where assets live, what is stored in content, and what runtime artifacts the module is responsible for generating.

### Group 1 Tasks

1. Standardize all asset probing on the host web-root contract:
   - metadata: `wwwroot/lib/fontawesome/metadata/`
   - css: `wwwroot/lib/fontawesome/css/`
   - svgs: `wwwroot/lib/fontawesome/svgs/`
   - webfonts: `wwwroot/lib/fontawesome/webfonts/`
2. Remove version-specific path assumptions from drivers and services.
3. Remove `node_modules` and local source-tree asset assumptions from module code and tracked docs.
4. Introduce a single internal asset resolution abstraction so all services use the same source of truth.
5. Define explicit failure behavior when assets are missing:
   - editor diagnostics should report the problem clearly
   - picker endpoints should return actionable errors
   - rendering should fail closed without throwing page-breaking exceptions

### Group 1 Deliverables

- one Orchard-native asset location contract
- one shared asset resolution service or options object
- updated documentation for required host asset placement

### Group 1 Validation

- build succeeds
- diagnostics can identify missing asset folders and files
- no service contains version-pinned probing logic

## Group 2. Enforce the canonical icon model

### Group 2 Goal

Make `family/style/name` the only supported icon identity model in v1.0.0.

### Group 2 Tasks

1. Remove free-form CSS resolution helpers and legacy compatibility code paths.
2. Update diagnostics terminology and counts to reflect canonical-only behavior.
3. Ensure field update, manifest generation, diagnostics, and rendering all use the same parser and validator.
4. Define strict normalization rules for:
   - family
   - style
   - icon name
5. Reject invalid canonical keys consistently at:
   - editor binding
   - manifest generation
   - runtime rendering

### Group 2 Deliverables

- a single canonical key policy implemented consistently across the module
- no v1 references to legacy or free-form icon storage in tracked code paths

### Group 2 Validation

- invalid keys render no output and are reported by diagnostics
- valid keys are normalized and round-trip correctly

## Group 3. Rebuild the metadata and curated manifest pipeline

### Group 3 Goal

Generate reliable, testable runtime artifacts from Font Awesome metadata provided by the host.

### Group 3 Tasks

1. Review and tighten metadata parsing against the actual Font Awesome 7 metadata shape.
2. Keep `normalized-icons.json` as the complete normalized catalog cache.
3. Keep `curated-icons.json` as the editor-facing curated manifest.
4. Keep `runtime-allowlist.json` as the render-time allowlist.
5. Ensure curated manifest items include all properties required by the picker and diagnostics, including:
   - key
   - family
   - style
   - name
   - label
   - unicode
   - search terms
   - free/pro indicator when derivable
6. Make manifest regeneration deterministic and idempotent.
7. Ensure settings publication is the single trigger for regeneration unless an explicit rebuild action is invoked.

### Group 3 Deliverables

- deterministic normalized catalog cache
- deterministic curated manifest
- deterministic runtime allowlist

### Group 3 Validation

- curated endpoint returns a complete JSON contract for the picker
- regeneration produces identical output when inputs have not changed
- artifact generation succeeds after settings changes

## Group 4. Stabilize the admin picker and field editing experience

### Group 4 Goal

Deliver the Orchard-native popup picker and field editing experience described by the current tracked decisions, aligned to the authoritative runtime contract.

### Group 4 Tasks

1. Fix the curated icons endpoint so the picker preview has the data it needs.
2. Audit the modal picker behavior for:
   - initial loading
   - empty state
   - error state
   - keyboard navigation
   - multiple field instances on one page
   - dynamic field injection scenarios
3. Ensure field editing supports only documented appearance options.
4. Confirm appearance settings are content-definition controlled and not embedded in picker selection.
5. Remove any assumptions that admin can use full vendored assets from module-local copies.
6. Ensure the picker can degrade gracefully when metadata exists but preview assets are incomplete.

### Group 4 Deliverables

- stable popup picker
- correct preview rendering
- size and color settings behavior aligned with field definition settings

### Group 4 Validation

- editor can choose, clear, re-open, cancel, and apply icons reliably
- preview glyphs render correctly for curated icons
- multiple icon fields on one editor screen behave independently

## Group 5. Replace the CSS generation approach with a production-safe implementation

### Group 5 Goal

Produce curated CSS in a way that is explicit, testable, and compatible with the host-provided Font Awesome assets.

### Group 5 Tasks

1. Remove stale `FontAwesome6*` font-family assumptions.
2. Decide whether v1 curated CSS generation will be:
   - parsed from the provided CSS bundle using a robust selector-aware approach, or
   - generated from metadata plus known templates for supported families and styles.
3. Standardize generated CSS naming and location.
4. Ensure generated CSS references only the required `@font-face` declarations and icon rules for the curated set.
5. Register generated CSS through Orchard resource management using a stable logical resource name.
6. Ensure generated CSS is optional when SVG mode is active, if that is part of the agreed v1 design.

### Group 5 Deliverables

- reliable curated CSS generation pipeline
- Orchard resource registration for generated output
- documentation for generated artifact names and locations

### Group 5 Validation

- generated CSS renders curated icons correctly with host assets
- generated CSS excludes unrelated families and icons
- no minified-line-splitting heuristics remain in the implementation

## Group 6. Define and implement the runtime rendering strategy

### Group 6 Goal

Make runtime rendering explicit, testable, and aligned with the module README.

### Group 6 Tasks

1. Define the v1 rendering modes and their activation rules.
2. Ensure `IBerelainRenderService` validates keys against `runtime-allowlist.json` before rendering.
3. Confirm the default renderer output contract:
   - webfont `<i>` output
   - SVG sprite output
   - or feature-driven selection between the two
4. Ensure display drivers, shapes, and any future parts use the same render service.
5. Fail closed when a key is invalid or outside the allowlist.
6. Add extension seams for future Liquid, Tag Helper, or shape-based rendering if not implemented immediately.

### Group 6 Deliverables

- a documented and implemented runtime rendering contract
- consistent render-path validation

### Group 6 Validation

- allowed canonical keys render correctly
- invalid or disallowed keys render no output
- rendering mode selection is deterministic

## Group 7. Define reusable Orchard content surfaces

### Group 7 Goal

Make the module reusable by other Orchard solutions through Orchard-native fields and parts rather than theme-specific assumptions.

### Group 7 Tasks

1. Keep `BerelainIconField` as the core identity field.
2. Decide whether v1 also includes a reusable `BerelainIconPart`.
3. If a part is included, define its responsibilities narrowly and keep rendering logic in services and display drivers.
4. Ensure migration and admin registration are Orchard-native and module-scoped.
5. Avoid creating CPCA-specific content types or display templates.

### Group 7 Deliverables

- reusable field definition
- optional reusable part if approved during ambiguity resolution

### Group 7 Validation

- the field can be attached to arbitrary content definitions
- any included part behaves independently of a specific theme

## Group 8. Diagnostics, observability, and operational readiness

### Group 8 Goal

Make the module operable in real environments where assets, curation, and rendering may drift.

### Group 8 Tasks

1. Update diagnostics to reflect canonical-only usage.
2. Report missing assets, unsupported families/styles, and invalid selected keys clearly.
3. Expose whether curated artifacts are current with current settings.
4. Make diagnostics useful for future SVG and usage optimization work without preserving legacy/free-form semantics.
5. Document what administrators should check when icons do not appear.

### Group 8 Deliverables

- actionable diagnostics endpoint and behavior
- operational documentation for troubleshooting

### Group 8 Validation

- diagnostics reveals stale artifacts, invalid keys, and missing assets
- logs and endpoint outputs are understandable without source inspection

## Group 9. Documentation and packaging cleanup

### Group 9 Goal

Bring tracked documentation into alignment with the authoritative module behavior before release.

### Group 9 Tasks

1. Remove or retire stale historical design documents that no longer reflect the tracked implementation direction.
2. Remove historical CPCA-specific references from tracked module documentation where they are no longer useful.
3. Ensure both README files match their intended scope and the final rendering and asset contracts.
4. Document the host provisioning requirements and the fact that licensed assets are external dependencies.
5. Review package metadata and embedded asset exclusions for release readiness.

### Group 9 Deliverables

- coherent tracked documentation set
- release-ready packaging guidance

### Group 9 Validation

- no tracked documentation contradicts the authoritative README
- external dependency policy is explicit and consistent

## Testing Plan

The module currently lacks a test project. A production-ready remediation should add targeted automated coverage.

### Minimum automated coverage

1. canonical key parsing and normalization
2. allowlist family/style filtering
3. metadata normalization from sample Font Awesome metadata
4. curated manifest generation
5. runtime allowlist generation
6. CSS generation behavior for a representative curated subset
7. render service allowlist enforcement

### Manual verification

1. enable module and configure settings in Orchard
2. attach `BerelainIconField` to a content definition
3. open editor and verify popup picker behavior
4. save and publish content with curated icons
5. verify rendered output in configured rendering mode
6. verify diagnostics output after content usage exists
7. verify behavior when required asset folders are missing or incomplete

## Pre-Implementation Decisions

The following items have now been clarified sufficiently to guide implementation.

### A1. Primary v1 rendering mode

- Admin editor tooling should use the strategy that best integrates with Orchard editor patterns and reliable preview behavior.
- Frontend rendering should target Font Awesome `SVG + JS` as the primary mode.
- Stored canonical keys must remain rendering-mode agnostic.
- If `SVG + JS` proves materially risky during v1 delivery, fallback frontend rendering may use CSS and webfonts without changing content storage.

### A2. Generated CSS artifact contract

- Generated artifacts must be tenant-scoped.
- Generated artifacts must not be written into shared `wwwroot` paths.
- Private artifact storage should live in tenant-scoped application data.
- Public delivery should use module endpoints or resource URLs.
- The implementation direction is route-based public access for artifacts such as curated CSS and sprite output.

### A3. Reusable Orchard surface beyond the field

- v1 ships only `BerelainIconField`.
- `BerelainIconPart` is deferred.
- WYSIWYG icon insertion is a separate future enhancement and not part of the initial v1 surface.

### A4. Appearance customization scope for v1

- v1 should use structured appearance controls rather than free-form style or class entry.
- Recommended v1 structured scope includes size and color.
- Width behavior should not be an editor-level choice in v1.
- Default width behavior should follow Font Awesome v7 defaults.
- If width configurability becomes necessary, it should be introduced as a field-definition or rendering concern rather than per-item editor data.
- Animation is a stretch goal if it can be modeled cleanly without destabilizing the core delivery.
- WYSIWYG editor styling support is outside the initial field implementation scope.

### A5. Diagnostics scope for v1

- The required v1 diagnostics deliverable is an Admin report of icons in use.
- Additional operational signals such as asset health and artifact freshness may be added later, but they are not required for the initial v1 diagnostics surface.

## Decision Recording Rule

For each ambiguity above:

1. record the question in `DISCUSSION.md`
2. record the agreed decision in `DISCUSSION.md`
3. migrate the final decision into tracked documentation before implementation begins

Implementation must not start on any group whose prerequisite decision has not yet been migrated into tracked documentation.

## Recommended Execution Order

1. Apply the recorded decisions for A1 through A5 during implementation and documentation updates.
2. Complete Group 1 and Group 2.
3. Complete Group 3 and Group 4.
4. Complete Group 5 and Group 6.
5. Complete Group 7 and Group 8.
6. Finish Group 9 and release-readiness validation.

## Definition of Ready

Work may begin when:

- the pre-implementation decisions are reflected consistently in tracked documentation
- the authoritative README and plan no longer conflict on current scope
- the host asset contract is accepted
- the canonical key-only policy is accepted

## Definition of Done

The remediation is complete when:

- all runtime paths use the authoritative host asset contract
- all stored icon references are canonical-only
- the picker, manifest pipeline, and render service operate on one consistent data contract
- curated runtime artifacts are generated deterministically
- diagnostics reflect the final v1 behavior
- tracked documentation matches the shipped implementation
