# Phase 2 PM Next Implementation Threads

## Review Date

2026-07-05

## Current Verified Baseline

Phase 2 is correctly positioned as a first-person 3D Unity prototype built over a hidden Frame / Slot / Tile / Field construction layer.

Verified or documented as working:

- Canonical hidden Field generation: `26 x 26`, 676 Slots, 1000 Tile bank, 324 unused Tiles.
- Tile `000` reserved for player/home spawn.
- Tile `666` reserved for pursuer/threat origin.
- Orientation indices `0-5`.
- Field Slot Report logging with seed `12345`.
- Dedicated 7-hex terrain frame test using 100m flat-to-flat hexes.
- Debug display for centers, vertices, edge midpoints, inner points, and hex outlines.
- Shared point deduplication by world position in the 7-hex terrain frame test.

Production constraint:

- Player-facing experience must be a snowy low-poly first-person forest.
- Hidden construction and debug overlays may expose grids, points, labels, tile IDs, and reports only in debug/test scenes or debug mode.

## Near-Term Production Goal

Move from hidden construction proof to a walkable 7-hex 3D terrain proof.

The next slice should answer:

Can the project generate a continuous low-poly snowy terrain surface from shared height points, preserve neighbor edge continuity, and allow a first-person player to walk across the center hex and its six neighbors without seeing board-game presentation?

## Thread 1: Shared Height-Point Terrain Data

Purpose:

Turn the current 7-hex debug point display into a reusable terrain data graph.

Scope:

- Create a data-backed representation for the radius-1 terrain frame.
- Store shared height points with stable IDs, kind, world position, height value, and membership in one or more Slots.
- Preserve the rule that Frame / Slot points / terrain points do not rotate.
- Assign deterministic prototype heights from seed or simple authored rules.
- Keep debug spheres and labels as a view of the data, not the source of truth.

Out of scope:

- First-person controller.
- Rune, stamina, chill, pursuer, or final forest props.
- Full 26 x 26 terrain generation.

Acceptance checks:

- Rebuilding the 7-hex test creates one shared height-point graph.
- Boundary points shared by neighboring hexes have one point ID and one height value.
- Changing a shared boundary point height affects every adjacent hex that uses it.
- Console report includes point count, Slot count, and shared-boundary validation result.
- Debug visuals can still show centers, vertices, edge midpoints, inner points, and height labels.

Recommended files / areas:

- `Assets/LostForest/Scripts/World/SharedHeightPoint.cs`
- `Assets/LostForest/Scripts/World/SevenHexTerrainFrameDebugView.cs`
- New world data classes under `Assets/LostForest/Scripts/World`

## Thread 2: Walkable 7-Hex Terrain Mesh

Purpose:

Generate an actual low-poly terrain mesh from the shared height-point graph.

Scope:

- Build a mesh for the center hex plus six neighbors.
- Use center, vertices, edge midpoints, and inner points as mesh vertices.
- Apply shared height values consistently across Slot boundaries.
- Add `MeshFilter`, `MeshRenderer`, and `MeshCollider`.
- Use a simple snow/prototype material.
- Keep debug lines and point markers toggleable.

Out of scope:

- Tile content rotation.
- Trees, rocks, logs, standing stones, runes.
- Large world streaming.

Acceptance checks:

- The generated surface is visibly 3D and low-poly.
- No cracks appear between neighboring hexes.
- A collider exists and matches the generated mesh closely enough for walking tests.
- Debug outlines can be hidden so the scene reads as terrain, not a board.
- The scene still supports a debug mode that reveals the hidden construction points.

Recommended files / areas:

- New terrain mesh builder under `Assets/LostForest/Scripts/World`
- Existing 7-hex terrain frame scene bootstrap
- Prototype material under `Assets/LostForest/Materials`

## Thread 3: First-Person Walk Test

Purpose:

Validate scale, slope feel, and basic movement across the 100m hex terrain.

Scope:

- Add a simple first-person controller with mouse look, walk, sprint placeholder, gravity, and ground detection.
- Spawn the player at the center/home hex.
- Test walking across all seven terrain hexes.
- Add a debug readout or log for current hidden Slot.
- Evaluate whether 100m flat-to-flat feels correct for embodied navigation.

Out of scope:

- Full stamina/chill economy.
- Rune interaction.
- Pursuer behavior.
- Final camera effects or animation.

Acceptance checks:

- Player starts on the center/home area.
- Player can traverse from center to every neighboring hex.
- Player does not fall through boundary seams.
- Current hidden Slot can be queried for debug.
- Debug grid/labels can be disabled for a player-facing view.

Recommended files / areas:

- `Assets/LostForest/Scripts/Player`
- `Assets/LostForest/Scripts/World`
- `Assets/LostForest/Scripts/Debug`

## Thread 4: Terrain-to-Tile Presentation Boundary

Purpose:

Make the separation between fixed terrain/frame geometry and rotating tile content explicit before props are added.

Scope:

- Define where terrain generation lives versus tile content placement.
- Add a minimal tile-content anchor model for non-terrain objects.
- Confirm tile rotation affects content anchors only.
- Add debug visualization for content anchor rotation.
- Use the 7-hex scene or a small generated Field slice to prove the rule.

Out of scope:

- Full tile definition authoring.
- Large prop library.
- Landmark/rune gameplay.

Acceptance checks:

- Terrain height points and mesh are unchanged when tile rotation changes.
- Content anchors rotate in 60-degree steps according to tile orientation.
- Debug output clearly reports Slot, Tile ID, orientation index, and anchor positions.
- The player-facing terrain remains continuous and natural.

Recommended files / areas:

- `Assets/LostForest/Scripts/Tiles`
- `Assets/LostForest/Scripts/World`
- `Docs/Architecture/Phase2_Board_Tile_Construction_Spec.md`

## Thread 5: Home Landmark Placeholder

Purpose:

Create the first player-facing landmark once the walkable terrain slice exists.

Scope:

- Place a simple standing-stone/home site on Tile `000` or the center test Slot.
- Use low-poly primitive stones or placeholder prefabs.
- Keep the site visible as a navigational anchor from nearby terrain.
- Mark it as home in hidden/debug data.

Out of scope:

- Rune deposit mechanics.
- Final art.
- Ritual progression effects.
- Pursuer escalation.

Acceptance checks:

- Standing stones appear in the player-facing view.
- Hidden/debug state identifies the home Slot / Tile `000`.
- Debug labels can be toggled without removing the landmark.
- The landmark does not look like board-game UI.

Recommended files / areas:

- `Assets/LostForest/Scripts/World`
- `Assets/LostForest/Scripts/Tiles`
- `Assets/LostForest/Prefabs/StandingStones`

## Recommended Execution Order

1. Shared Height-Point Terrain Data.
2. Walkable 7-Hex Terrain Mesh.
3. First-Person Walk Test.
4. Terrain-to-Tile Presentation Boundary.
5. Home Landmark Placeholder.

Do not start rune, stamina/chill, or pursuer implementation until the player can walk across a continuous 7-hex terrain slice.

## Management Notes

- Keep the 7-hex test as the active terrain proving ground.
- Keep full `26 x 26` Field generation intact as the hidden-world proof.
- Avoid importing Phase 1 board presentation into player-facing scenes.
- Treat debug visualization as a switchable view over real data.
- Prefer small vertical slices that can be played in Unity over broad systems with no embodied test.
