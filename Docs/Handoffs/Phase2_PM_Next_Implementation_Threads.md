# Phase 2 PM Next Implementation Threads

## Review Date

2026-08-10

## Current Baseline

The active development baseline is the integrated first-person prototype in:

- `Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity`

The World's End / Frost Barrier milestone is preserved in commit `7cd7e14` on
`codex/frost-barrier-sunlight`. Repository and scene cleanup continues on
`codex/project-cleanup`.

The local workspace should contain one canonical project clone at
`/Users/klove/Documents/LostForestPhase2`. Do not resume work from detached or
older milestone worktrees.

## Completed Prototype Spine

- Canonical hidden `26 x 26` Field generation.
- First-person terrain traversal and active-region rendering.
- Player-centered fog and forest readability content.
- Sprint, stamina, chill, frozen, and game-over pressure states.
- Rune selection, pickup, carry, and Home deposit loop.
- Home and world landmark prototypes.
- World's End boundary detection and three-ring Frost Barrier presentation.
- Frost exposure, movement decline, chill pressure, boundary clamp, and recovery.

Player-facing presentation must remain a snowy low-poly forest. Hidden Slot,
Tile, axial, and address data stay available to gameplay and debug systems but
must not become board-game presentation in the normal player view.

## Next Milestone: Light and Shadow

Start Light and Shadow from the completed cleanup branch, not from the old
detached Light/Shadow worktree.

Primary goal:

Make light a readable navigation and pressure language while preserving fog,
landmarks, runes, terrain readability, and laptop-friendly performance.

Initial scope:

- Establish a stable daylight and ambient baseline in the integrated scene.
- Make Home and landmark silhouettes readable without exposing the hidden grid.
- Define a lightweight shadow budget for terrain, trees, landmarks, runes, and
  frost-ring content.
- Add Light/Shadow settings through the existing scene bootstrap so repairs are
  deterministic.
- Confirm the frost vignette and fog remain readable across the lighting range.

Acceptance checks:

- The player can orient toward Home and nearby landmarks using shape and light.
- Fog still controls reveal distance and does not expose whole hidden Tiles.
- Frost territory remains visually distinct at the Field edge.
- Rune and condition feedback remain legible.
- No visible hex outlines, Tile IDs, Slot addresses, or board presentation appear
  in the player-facing view.
- Performance remains suitable for the current laptop target.
- The integrated scene bootstrap and validation complete without compile or
  missing-reference errors in a licensed Unity editor session.

## Branch and Scene Discipline

- Create the Light/Shadow branch from the merged `codex/project-cleanup` head.
- Keep one active integrated prototype scene unless a durable test requires more.
- Commit one milestone at a time and push it before creating dependent tasks.
- Treat Git history as the archive for retired milestone scenes.
- Do not delete or replace the canonical clone while Unity has the project open.
