# Config Guide

## Strict Pool Behavior

### `ForceOnlySelected` + `OutfitFirstBudgetStrategy`
- UI path:
  - Advanced -> General -> `仅从装备池选择`
  - Advanced -> General -> `先成套后升材质`
- Runtime behavior:
  - If core outfit planning succeeds, outfit-first behavior is unchanged.
  - If core outfit planning fails or core generation becomes incomplete, strict pool mode no longer keeps vanilla apparel.
  - The system strips existing apparel and continues trying to wear only valid configured apparel from the selected pools/lists.
  - Empty slots are allowed when the configured pool cannot satisfy all layers.
  - Log output distinguishes planning failure, strict-pool fallback, and "no valid apparel candidates".

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
- If any core layer has no valid candidate, apparel apply fails fast before stripping when `ForceOnlySelected` is disabled.
- If `ForceOnlySelected` is enabled, the same failure switches to strict pool fallback instead of preserving vanilla apparel.
- Strategy budget uses max range value (`ApparelMoney.max`).
- If core total exceeds budget, the strategy auto-replaces/downgrades pieces until it fits budget when feasible.
- Core armor value (middle+shell) is constrained to at least 60% of budget by default when feasible.
- Material upgrades are applied only to core items and only change material (no item-type/quality change).
- Supplemental apparel still follows budget checks and cannot replace protected core layers.

### Budget UI behavior
- Advanced -> General displays current kind default apparel budget source (`PawnKindDef.apparelMoney`) for reference.
- Manual override range for apparel/weapon budget sliders is `0..10000`.

## Reset Sync Behavior

- `Reset Current Kind`: keeps target `KindGearData.isModified == false` after reset and immediately syncs to save-scoped settings.
- `Load Default Faction`: removes current faction override, reloads defaults, and immediately syncs to save-scoped settings.
- `Reset Current Faction`: removes current faction override and immediately syncs to save-scoped settings.

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
