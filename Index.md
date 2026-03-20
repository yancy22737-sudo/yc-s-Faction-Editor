# Index

## Strict Pool And Kind Enumeration

- `FactionGearModification/Managers/GearApplier.cs`
  - Responsibility: Keep `ForceOnlySelected` authoritative even when outfit-first core planning fails.
  - Behavior: If core outfit planning or initial generation fails and `ForceOnlySelected` is enabled, worn apparel is stripped and the system falls back to equipping only valid items from the configured apparel pools; empty slots are allowed.
  - Logging: Distinguishes core planning failure, strict-pool fallback application, and "no valid apparel candidates" terminal state.
  - Hediff safety: `ApplyHediffWithPart` and `GetUserSpecifiedBodyParts` now fail fast on missing pawn health/body data instead of throwing.
- `FactionGearModification/UI/FactionGearEditor.cs`
  - Responsibility: Provide a stable, de-duplicated PawnKind list for editor, preview, batch apply, and "apply to others".
  - Sources: `pawnGroupMakers.options`, `traders`, `carriers`, `guards`, plus already-configured `kindGearData` entries for the faction.
  - Contract: Return type, cache key, and sort order remain unchanged.

## Apparel Budget Strategy

- `FactionGearModification/Managers/GearApplier.cs`
  - Responsibility: Apply two-phase apparel budget strategy when `KindGearData.OutfitFirstBudgetStrategy` is enabled.
  - Phase 1: Build and equip a complete core outfit first (`TorsoInner`, `LegsInner`, `ArmorMiddle`, `Shell`) using max apparel budget.
  - Over-budget handling: If core plan exceeds budget, auto-replace/downgrade pieces until budget is met when feasible.
  - Armor floor: Keep core armor value (`ArmorMiddle` + `Shell`) at least 60% of budget by default.
  - Phase 2: Upgrade core item materials using remaining budget without changing item def/quality.
  - Fail Fast: Core feasibility is validated before stripping worn apparel. If any core layer cannot be satisfied, apparel apply exits early and current worn apparel is preserved.
- `FactionGearModification/Data/KindGearData.cs`
  - Responsibility: Persist per-kind switch `OutfitFirstBudgetStrategy` (default `true`), including save/load and deep-copy paths.
- `FactionGearModification/UI/Panels/GearEditPanel.cs`
  - Responsibility: Expose `OutfitFirstBudgetStrategy` in Advanced General settings.
- `FactionGearModification/UI/Dialog_BatchApply.cs`
  - Responsibility: Copy `OutfitFirstBudgetStrategy` in General batch-copy category.

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
