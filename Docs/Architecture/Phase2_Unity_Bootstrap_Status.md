# Phase 2 Unity Project Status

## Review Date

2026-08-10

## Active Prototype Scene

Lost Forest Phase 2 now uses one integrated prototype scene:

- `Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity`

Create or repair it with:

- `Lost Forest > Bootstrap > Create or Repair Grid Movement Fog Test Scene`

The scene is also the only enabled scene in `ProjectSettings/EditorBuildSettings.asset`.

Earlier Hidden Field, Tile Construction, 7 Hex Terrain Frame, Early WalkThru, and
Landmarks milestone scenes were removed after their systems were integrated into
the active prototype. Their history remains available in Git through commit
`7cd7e14` and earlier commits.

## Integrated Baseline

The active scene currently provides:

- A canonical hidden `26 x 26` Field with 676 Slots.
- Tile `000` reserved for Home and Tile `666` reserved for the pursuer origin.
- Player-centered terrain generation and active-region rendering.
- First-person movement, terrain grounding, slope response, sprint, stamina,
  chill, frozen, and game-over condition states.
- Player-centered fog and prototype birch readability content.
- Rune selection, pickup, carrying, and Home deposit flow.
- Home and world landmark prototypes.
- World's End detection beyond the playable Field.
- Up to three rendered frost rings outside the Field.
- Frost exposure, chill pressure, movement decline, boundary clamping, and a
  lingering frost vignette after returning to the Field.
- A debug HUD for hidden Slot, travel, condition, rune, landmark, and frost state.

## Validation

The Frost Barrier milestone passed its Unity editor validation before the clean
clone was created. The reported checks included:

- Field size `26 x 26`.
- Home and pursuer reservations.
- Active region rendering.
- Three outer frost rings.
- Playable movement and terrain grounding.
- Rune marker and condition state initialization.
- World's End ring and terrain-elevation sampling.

Terminal batch validation can still stop during Unity licensing initialization.
That environment failure is separate from project compilation and scene
validation. After repository cleanup, run the active scene validation from an
already licensed Unity editor session before merging the cleanup branch.

## Operating Rule

Use the integrated scene for all new gameplay work. Add a separate scene only
when it provides a durable automated or visual test that cannot reasonably live
in the integrated prototype. Temporary milestone copies should remain in Git
history instead of accumulating in `Assets/LostForest/Scenes`.
