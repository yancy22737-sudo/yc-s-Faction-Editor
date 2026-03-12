# Config Guide

## Hediff Target Part Mode

### Auto mode
- Stored state:
  - `ForcedHediff.partsDefNames == null` or empty
- Runtime behavior:
  - Target parts resolved by strict order:
    1. recipe fixed parts/groups
    2. explicit mapping
    3. inferred mapping
  - If no valid target exists, the hediff is skipped with a warning log.
  - No whole-body random fallback is used.

### Manual mode
- Stored state:
  - `ForcedHediff.partsDefNames` contains one or more `BodyPartDef.defName` values.
- Runtime behavior:
  - Only selected parts are considered.
  - Invalid/missing/unavailable parts are filtered out.
  - If no valid selected part remains, application is skipped with warning.

## Safety Rules

- Added-part/implant hediffs are allowed.
- Missing-part and obvious lethal-risk definitions remain blocked.
- Incapacitation checks are applied to both manual and auto part paths.

## Logging

- Validation warnings/errors and part-resolution failures are logged with `[FactionGearCustomizer]` prefix.
- Runtime behavior is non-blocking (no modal popup) to avoid interrupting generation pipelines.
