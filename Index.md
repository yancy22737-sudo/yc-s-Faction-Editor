# Index

## Native Translation Pipeline

- `FactionGearModification/Managers/LanguageManager.cs`
  - Responsibility: Legacy wrapper only. All runtime text now resolves through RimWorld native `Translate()` instead of private XML loading.
  - Depends on: `Verse.LanguageDatabase`, `Verse.Translate()` extension pipeline.
  - Contract: Preserve existing `LanguageManager.Get(...)` call sites during migration while allowing third-party translation mods to override `Keyed/DefInjected` resources normally.
  - Fallback: When active-language text is missing, uses this mod's keyed file fallback chain. Default order is `CurrentLanguage -> English`; Traditional Chinese uses `ChineseTraditional -> ChineseSimplified -> English`.
- `1.6/Languages/English/Keyed/FactionGearCustomizer.xml`
  - Responsibility: English keyed text source for UI, messages, validation text, and wiki text.
- `1.6/Languages/ChineseSimplified/Keyed/FactionGearCustomizer.xml`
  - Responsibility: Simplified Chinese keyed text source for UI, messages, validation text, and wiki text.
- `1.6/Languages/*/DefInjected/...`
  - Responsibility: Keep Def-backed translations on the native RimWorld path.
  - Contract: External translation mods should override this mod by providing standard `Languages/<lang>/Keyed` and `Languages/<lang>/DefInjected` files only.

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
  - Responsibility: Show current kind default apparel budget source (`PawnKindDef.apparelMoney`) to guide manual tuning.
  - Contract: Manual apparel/weapon budget sliders support range `0..10000`.
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

## Reset And Override Hygiene

- `FactionGearModification/UI/Panels/TopBarPanel.cs`
  - Responsibility: `Reset -> 当前 Pawn / 当前 Faction / Load Default Faction` now only touches the current target and must not call `PerformDeepCleanup`.
  - Contract: Only `ResetEVERYTHING` may clear preset binding, editor session, and save-scoped custom state.
  - Contract: `Reset -> 当前兵种` must keep target `KindGearData.isModified == false` after reset to avoid false modified marker.
  - Contract: Reset actions (`当前兵种/当前派系/加载默认派系`) must immediately run save-sync flow so `savedFactionGearData` is updated without extra manual save.
- `FactionGearModification/Managers/FactionGearManager.cs`
  - Responsibility: `LoadKindDefGear` now fail-fasts on null `PawnKindDef` / `KindGearData` instead of throwing.
- `FactionGearModification/UI/FactionGearEditor.cs`
  - Responsibility: Prune empty `KindGearData` overrides before preset save and save-sync so blank runtime objects cannot override vanilla rules.
- `FactionGearModification/Data/FactionGearData.cs`
  - Responsibility: Owns explicit `KindGearData` removal and default-override pruning while keeping list/dictionary indexes in sync.
- `FactionGearModification/Core/FactionGearCustomizerSettings.cs`
  - Responsibility: Owns explicit faction-level removal so target-scoped reset can delete the active override without clearing unrelated preset/save state.
- `FactionGearModification/UI/Panels/ItemLibraryPanel.cs`
  - Responsibility: Read-only drawing must not create current-kind override data as a side effect.
