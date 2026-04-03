# Two Rivers Berelain Module

`TwoRivers.Berelain` is a standalone Orchard Core module for selecting, curating, and rendering Font Awesome icons in Orchard Core applications.

## What the Module Provides

- tenant-scoped Font Awesome settings
- curated icon selection by family and style
- `BerelainIconField` for canonical icon storage
- admin icon picker with visual preview
- canonical icon key format: `family/style/name`
- diagnostics for icons in use
- frontend rendering support using the module's configured runtime strategy

## Requirements

### Host-Provided Assets

The consuming Orchard Core application must provide Font Awesome assets under:

- `wwwroot/lib/fontawesome/`

Expected asset layout:

```text
wwwroot/
└─ lib/
   └─ fontawesome/
      ├─ css/
      ├─ js/
      ├─ metadata/
      ├─ scss/
      ├─ sprites/
      ├─ svgs/
      ├─ svgs-full/
      └─ webfonts/
```

Font Awesome assets are an external dependency and are not distributed with this module or its NuGet package.

## Installation

1. Reference `TwoRivers.Berelain` from your Orchard Core application.
2. Provision Font Awesome assets under `wwwroot/lib/fontawesome/` in the host application.
3. Enable the `TwoRivers.Berelain` feature for the tenant.
4. Enable any companion rendering feature required by your chosen runtime strategy.

## Configuration

Open `Admin -> Configuration -> Settings -> Font Awesome` and choose the families and styles allowed for the tenant.

Saving settings regenerates the tenant-scoped runtime artifacts used by the picker, diagnostics, and frontend rendering.

## Using the Icon Field

Attach `BerelainIconField` to the content type or content part that needs icon selection.

The editor experience provides:

- visual preview of the current icon
- popup picker for curated icons
- canonical key storage
- structured appearance controls supported by the field definition

## Canonical Icon Keys

Icons are stored using the format:

- `family/style/name`

Examples:

- `classic/solid/house`
- `classic/solid/user-check`
- `sharp/solid/phone`

The module treats canonical keys as the single supported icon identity format.

## Diagnostics

The module provides an admin report of icons in use so administrators can validate curated usage and review actual icon selections in content.

## Rendering Notes

- Admin tooling uses the rendering strategy that best supports Orchard editor integration and reliable preview behavior.
- Frontend rendering uses the module's configured runtime strategy while remaining canonical-key based.
- Stored icon values are rendering-mode agnostic.

## License

BSD-3-Clause - see [LICENSE.txt](LICENSE.txt).