# Config Guide

## Outfit-First Budget Strategy

### Switch
- `KindGearData.OutfitFirstBudgetStrategy` (default: `true`)
- UI path:
  - Advanced -> General -> `Outfit First, Material Upgrade Later`

### Runtime behavior
- Core outfit is generated before supplemental apparel:
  1. Torso inner layer (`OnSkin/Middle`)
  2. Legs inner layer (`OnSkin/Middle`)
  3. Torso armored middle layer (`Middle`, `ArmorRating_Sharp > 0.4`)
  4. Torso shell layer (`Shell`)
- If any core layer has no valid candidate, apparel apply fails fast before stripping and keeps current worn apparel unchanged.
- Strategy budget uses max range value (`ApparelMoney.max`).
- If core total exceeds budget, the strategy auto-replaces/downgrades pieces until it fits budget when feasible.
- Core armor value (middle+shell) is constrained to at least 60% of budget by default when feasible.
- Material upgrades are applied only to core items and only change material (no item-type/quality change).
- Supplemental apparel still follows budget checks and cannot replace protected core layers.

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
