# API Notes

## Runtime Apparel Budget API

### `GearApplier.ApplyApparel`
- Behavior when `KindGearData.OutfitFirstBudgetStrategy == true`:
  - Validates core-layer feasibility before stripping worn apparel (fail-fast).
  - Uses max apparel budget (`ApparelMoney.max`) as the strategy budget.
  - Equips core layers first:
    1. `TorsoInner` (`Torso` + `OnSkin/Middle`)
    2. `LegsInner` (`Legs` + `OnSkin/Middle`)
    3. `ArmorMiddle` (`Torso` + `Middle` + `ArmorRating_Sharp > 0.4`)
    4. `Shell` (`Torso` + `Shell`)
  - If initial core plan exceeds budget, pieces are replaced/downgraded until budget-compliant when feasible.
  - Default armor floor: core armor value is constrained to at least `0.60 * budget` when feasible.
  - Performs material-only upgrades for equipped core items using remaining budget.
  - Applies supplemental apparel with normal budget checks and core-layer conflict protection.

### `KindGearData.OutfitFirstBudgetStrategy`
- Type:
  - `bool`
- Default:
  - `true`
- Persistence:
  - Serialized via `Scribe_Values.Look(..., "outfitFirstBudgetStrategy", true)`.
- Copy semantics:
  - Included in `DeepCopy`, `CopyFrom`, and batch apply `General` category copy.

## Runtime Hediff Application API

### `HediffApplicationService.ApplyForcedHediff`
- Signature:
  - `ApplyForcedHediff(Pawn pawn, ForcedHediff forcedHediff, Func<HediffPoolType, HediffDef> poolSelector = null)`
- Behavior:
  - Resolves pool entry to a concrete `HediffDef` when needed.
  - Runs `HediffApplicationValidator.ValidateApplicationToPawn`.
  - Rejects unsafe hediffs (`MissingPart`, fatal/tendable patterns, severe capacity drops).
  - Routes to part/no-part application path.
  - Applies incapacitation checks uniformly for all application paths.

### `HediffApplicationService.IsHediffSafe`
- Signature:
  - `IsHediffSafe(HediffDef def)`
- Behavior:
  - Allows implant/added-part hediffs.
  - Rejects explicit missing-part and severe-risk definitions.

## Body Part Resolution API

### `HediffPartResolver.ResolveCandidates`
- Signature:
  - `ResolveCandidates(Pawn pawn, HediffDef hediffDef, ForcedHediff forcedHediff)`
- Return:
  - `PartResolutionResult`
    - `Source`: `Manual/Recipe/Mapping/Inference/None`
    - `Candidates`: filtered valid body part records
    - `Warnings`: non-fatal diagnostic messages
- Priority order:
  1. `ForcedHediff.parts` (manual)
  2. `RecipeDef.addsHediff` fixed part/group
  3. Explicit mapping (`HediffPartMappingManager.GetAllMappings`)
  4. Inferred mapping (`GetRecommendedPartsForHediff`)

## Data Contract (unchanged)

### `ForcedHediff`
- Persisted fields:
  - `hediffDefName`
  - `partsDefNames`
  - `PoolType`, `maxParts`, `maxPartsRange`, `chance`, `severityRange`
- Compatibility:
  - Empty/null `partsDefNames` means auto target-part mode.
  - No migration required for old saves.
