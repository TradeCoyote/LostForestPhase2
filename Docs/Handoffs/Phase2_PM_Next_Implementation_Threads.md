# Phase 2 PM Next Implementation Threads

## Review Date

2026-08-10

## Current Baseline

The active development baseline is the integrated first-person prototype in:

- `Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity`

The World's End / Frost Barrier milestone is preserved in commit `7cd7e14`.
Repository cleanup is preserved in `7559a00`. Light and Shadow is complete on
`codex/light-shadow`, and the three-rune victory loop continues on
`codex/win-condition`.

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
- Flat overcast lighting with randomized, brief cloud-thinning windows that do
  not leave players a persistent shadow compass.
- Three required rune stones, Home deposits, third-deposit victory, and a
  `Play Again? Y / N` run-end prompt.

Player-facing presentation must remain a snowy low-poly forest. Hidden Slot,
Tile, axial, and address data stay available to gameplay and debug systems but
must not become board-game presentation in the normal player view.

## Next Milestone: Pursuer Pressure

Start Pursuer Pressure from the completed and validated `codex/win-condition`
head. Do not resume from an older detached worktree.

Primary goal:

Create systemic pursuit pressure that escalates through time, player noise,
distance from Home, carried rune stones, and deposited rune progress while
keeping the pursuer mostly unseen.

Initial scope:

- Add dormant, interest, search, stalk, close-pressure, and catch states.
- Use hidden Slot distance and travel history for high-level pursuit decisions.
- React to sprinting, rune pickup, time away from Home, and deposit progress.
- Provide indirect cues through placeholder sound hooks, glimpses, fog pressure,
  or disturbed forest presentation rather than a fully readable enemy.
- Add debug state, distance, target Slot, and force-state controls.
- Connect a pursuer catch to the existing loss/restart flow without interfering
  with chill defeat or third-rune victory.

Acceptance checks:

- Pursuer state changes are deterministic and visible in debug mode.
- Pressure increases after a rune pickup and after each successful deposit.
- Normal player-facing mode communicates proximity without exposing exact state,
  position, path, or hidden grid information.
- A catch produces an understandable loss and can restart cleanly.
- Returning the third rune stone still wins before later pursuer updates can
  overwrite the result.
- The integrated scene bootstrap and validation complete without compile or
  missing-reference errors in a licensed Unity editor session.

## Branch and Scene Discipline

- Create the Pursuer branch from the completed `codex/win-condition` head.
- Keep one active integrated prototype scene unless a durable test requires more.
- Commit one milestone at a time and push it before creating dependent tasks.
- Treat Git history as the archive for retired milestone scenes.
- Do not delete or replace the canonical clone while Unity has the project open.
