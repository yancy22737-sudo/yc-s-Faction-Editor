# Index

## Project Map

### Core Runtime
- `FactionGearModification/Managers/GearApplier.cs`
  - Responsibility: Runtime entry for applying configured gear to generated pawns.
  - Depends on: `FactionGearGameComponent`, `FactionGearData`, `HediffApplicationService`.
- `FactionGearModification/Managers/HediffApplicationService.cs`
  - Responsibility: Unified hediff application orchestration (validation, safety checks, apply).
  - Depends on: `HediffApplicationValidator`, `HediffPartResolver`, RimWorld health APIs.
- `FactionGearModification/Managers/HediffPartResolver.cs`
  - Responsibility: Resolve target body parts in strict priority order.
  - Depends on: `RecipeDef`, `HediffPartMappingManager`, pawn body/health state.

### Validation
- `FactionGearModification/Validation/HediffApplicationValidator.cs`
  - Responsibility: Pre-apply validation for forced hediff entries and pawn compatibility.
- `FactionGearModification/Validation/HediffPartMappingManager.cs`
  - Responsibility: Explicit mappings, inference rules, per-part implant limits.

### UI
- `FactionGearModification/UI/Panels/HediffCardUI.cs`
  - Responsibility: Hediff card editing UI, including target-part mode and selection.
  - Depends on: `Dialog_BodyPartSelector`, `LanguageManager`, `UndoManager`.
- `FactionGearModification/UI/Dialogs/Dialog_BodyPartSelector.cs`
  - Responsibility: Manual target-body-part multi-select dialog.

### Data
- `FactionGearModification/Data/ForcedHediff.cs`
  - Responsibility: Serialized hediff config model (`hediffDefName`, `partsDefNames`, ranges/chance/pool).
- `FactionGearModification/Data/KindGearData.cs`
  - Responsibility: Per-pawn-kind aggregated gear configuration.

## Hediff Apply Flow

1. `Patch_GeneratePawn.Postfix` -> `GearApplier.ApplyCustomGear`.
2. `GearApplier.ApplyHediffs` iterates configured entries/pools.
3. `HediffApplicationService.ApplyForcedHediff`:
   - resolves pool candidate if needed,
   - validates via `HediffApplicationValidator.ValidateApplicationToPawn`,
   - applies strict safety checks,
   - resolves body parts via `HediffPartResolver`,
   - applies hediff with unified incapacitation guard.
4. `HediffPartResolver` priority:
   - manual `ForcedHediff.parts`,
   - recipe fixed body parts/groups,
   - explicit mapping (`HediffPartMappingManager`),
   - inferred mapping.

## Key Interfaces

- `HediffApplicationService.ApplyForcedHediff(Pawn, ForcedHediff, Func<HediffPoolType, HediffDef>)`
  - Unified runtime entry for single/pool hediff application.
- `HediffPartResolver.ResolveCandidates(Pawn, HediffDef, ForcedHediff)`
  - Returns `PartResolutionResult` with candidates, source, and warnings.
