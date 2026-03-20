# API Notes

## Strict Pool Fallback API

### `GearApplier.ApplyApparel`
- Additional behavior when both `KindGearData.ForceOnlySelected == true` and `KindGearData.OutfitFirstBudgetStrategy == true`:
  - If core outfit planning fails before stripping, the pipeline now switches to strict pool fallback instead of preserving vanilla apparel.
  - If core outfit generation starts but cannot complete all core layers, the pipeline keeps already-equipped pool items and continues equipping any remaining valid configured apparel.
  - Terminal outcome is pool-only: no fallback to vanilla apparel, empty slots are allowed.

### `FactionGearEditor.GetFactionKinds`
- Contract:
  - Returns `List<PawnKindDef>` with stable label-based sorting.
- Sources:
  - `pawnGroupMakers.options`
  - `pawnGroupMakers.traders`
  - `pawnGroupMakers.carriers`
  - `pawnGroupMakers.guards`
  - existing configured `FactionGearData.kindGearData` entries for the same faction
- Compatibility:
  - No signature change
  - No cache-key change
  - De-duplication remains `defName`-based

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
  - Advanced General UI now shows current kind default budget source from `PawnKindDef.apparelMoney`.
  - Manual budget override UI range is `0..10000` for apparel/weapon budgets.

### `KindGearData.OutfitFirstBudgetStrategy`
- Type:
  - `bool`
- Default:
  - `true`
- Persistence:
  - Serialized via `Scribe_Values.Look(..., "outfitFirstBudgetStrategy", true)`.
- Copy semantics:
  - Included in `DeepCopy`, `CopyFrom`, and batch apply `General` category copy.

## Reset Sync API

### `TopBarPanel.BuildResetMenuOptions`
- Behavior:
  - `Reset Current Kind`: resets selected `KindGearData`, reloads default kind gear, keeps `isModified == false`, then immediately runs save-sync flow.
  - `Load Default Faction`: removes current faction override, reloads faction defaults, then immediately runs save-sync flow.
  - `Reset Current Faction`: removes current faction override, then immediately runs save-sync flow.

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
