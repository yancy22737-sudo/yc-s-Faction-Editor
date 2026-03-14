# API Notes

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
