# Compact Top Bar Spec

## Why
In version v1.1.26 (screenshot provided), the interface has excessive whitespace at the top because the Title ("派系装备修改器") occupies a separate row from the Action Buttons. The current code (v1.1.29) has temporarily removed the title to save vertical space, which sacrifices context.

## What Changes
- Restore the Title ("FactionGearCustomizer") to the Top Bar.
- Arrange Title and Action Buttons in a single horizontal row (height: 40px).
- Ensure the Title is aligned to the left (after the Logo).
- Ensure Action Buttons flow to the right of the Title or are right-aligned.
- Optimize vertical alignment to prevent wasted space.

## Impact
- Affected specs: UI Layout.
- Affected code: `FactionGearModification/UI/Panels/TopBarPanel.cs`.

## ADDED Requirements
### Requirement: Single-Row Top Bar
The Top Bar SHALL display the Logo, Title, and Action Buttons in a single horizontal row.

#### Scenario: Success case
- **WHEN** user opens the Faction Gear window
- **THEN** the Title is visible next to the Logo
- **AND** the Action Buttons are visible on the same row to the right
- **AND** the total height of the Top Bar does not exceed 40px (plus minimal padding).

## MODIFIED Requirements
### Requirement: Title Visibility
The Title SHALL be visible in the Top Bar (restored from hidden state).

## REMOVED Requirements
None.
