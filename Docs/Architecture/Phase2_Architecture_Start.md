# Lost Forest Phase 2 - Architecture Start

## Purpose

This document defines the first clean architecture pass for Lost Forest Phase 2.

Phase 2 is not a visual remake of Phase 1. It is the functional 3D structure that Phase 3 can wrap in final art, animation, audio, and atmosphere.

The architecture should prove:

- A hidden 2D tile layout can drive a stitched 3D first-person world.
- Tile IDs and region logic from Phase 1 can survive in a 3D presentation.
- Runes, standing stones, stamina, chill, sprint, terrain pressure, landmarks, and pursuer pressure work as modular systems.
- Debugging and tuning are available from the beginning.

## Recommended Starting Point

Start with the tile/chunk world structure.

Reason:

The tile-to-chunk system is the main bridge between Phase 1 and Phase 2. Movement, runes, landmarks, and pursuer pressure all depend on knowing where the player is in the hidden tile world. If this layer is clean, every later system has a stable foundation.

Do not start with graphics, final forest art, monster behavior, or UI polish.

## Phase 1 Handoff Import Strategy

Phase 1 handoffs should be imported system by system, not all at once.

Use this order:

1. Board/tile layout logic.
2. Rune placement and return rules.
3. Stamina, chill, sprint, and movement cost rules.
4. Terrain/elevation meaning.
5. Landmark/navigation logic.
6. Pursuer pressure logic.
7. Debug tools and tuning assumptions.

For each imported handoff, extract:

- What the system proved.
- Which rules should survive.
- Which presentation details should be discarded.
- What data the Phase 2 version needs.
- What debug information the Phase 2 version needs.

## Core Architecture Principle

Separate hidden game logic from player-facing presentation.

The player should see:

- A snowy low-poly first-person forest.
- Standing stones.
- Landmarks.
- Runes.
- Environmental feedback.
- Minimal atmospheric UI.

The system should know:

- Tile IDs.
- Tile adjacency.
- Chunk coordinates.
- Region tags.
- Rune eligibility.
- Landmark slots.
- Terrain/elevation modifiers.
- Pursuer pressure values.
- Debug overlays.

## Runtime Layers

### 1. Tile Layout Layer

Hidden 2D representation of the world.

Responsibilities:

- Draw or generate tile IDs.
- Store tile coordinates.
- Store tile adjacency.
- Store home tile.
- Store rune-eligible tiles.
- Store terrain/region tags.
- Provide lookup by coordinate and ID.

This layer is allowed to feel like Phase 1 internally.

### 2. Chunk Presentation Layer

3D representation of the tile layout.

Responsibilities:

- Map tile IDs to 3D hex chunk prefabs or generated chunk recipes.
- Place chunks in world space.
- Stitch chunk edges cleanly.
- Load/activate nearby chunks around the player.
- Hide unloaded or distant chunks.
- Support placeholder visuals that can be replaced in Phase 3.

This layer should not own game rules. It presents the tile layout.

### 3. Player Simulation Layer

First-person movement and pressure systems.

Responsibilities:

- Walk.
- Sprint.
- Track stamina.
- Track chill.
- Apply terrain/slope/tile modifiers.
- Report player tile/chunk position.
- Emit movement/noise events for other systems.

### 4. Objective Layer

Rune and standing-stone gameplay.

Responsibilities:

- Spawn runes using tile rules.
- Let the player discover/collect/activate runes.
- Track carried rune state.
- Deposit runes at standing stones.
- Update run progress.
- Escalate pressure after progress.

### 5. Pursuer Pressure Layer

Systemic threat layer.

Responsibilities:

- Track pressure state.
- React to player movement, sprinting, rune progress, distance from home, and tile danger.
- Produce indirect feedback through audio, proximity, glimpses, or debug visualization.
- Avoid depending on final monster art.

### 6. Feedback and Debug Layer

Developer visibility and player communication.

Responsibilities:

- Show debug tile IDs.
- Show chunk boundaries.
- Show current tile.
- Show rune locations.
- Show stamina/chill values.
- Show pursuer state.
- Show world seed/layout.
- Keep debug UI separate from player-facing UI.

## Initial Unity Folder Structure

Create this structure once the Unity project exists:

- `Assets/LostForest/Scenes`
- `Assets/LostForest/Scripts/Core`
- `Assets/LostForest/Scripts/Tiles`
- `Assets/LostForest/Scripts/Chunks`
- `Assets/LostForest/Scripts/World`
- `Assets/LostForest/Scripts/Player`
- `Assets/LostForest/Scripts/Runes`
- `Assets/LostForest/Scripts/Pursuer`
- `Assets/LostForest/Scripts/Feedback`
- `Assets/LostForest/Scripts/Debug`
- `Assets/LostForest/Data/Tiles`
- `Assets/LostForest/Data/Runes`
- `Assets/LostForest/Data/Player`
- `Assets/LostForest/Data/Pursuer`
- `Assets/LostForest/Prefabs/Chunks`
- `Assets/LostForest/Prefabs/Landmarks`
- `Assets/LostForest/Prefabs/Runes`
- `Assets/LostForest/Prefabs/StandingStones`
- `Assets/LostForest/Materials/Prototype`

## First Manager Set

Keep the first manager set small.

### `GameManager`

Owns run state.

Initial duties:

- Start prototype run.
- Reset prototype run.
- Track rune deposits.
- Track win/failure state if enabled.

### `TileLayoutManager`

Owns hidden tile layout.

Initial duties:

- Generate a small test layout.
- Assign tile IDs.
- Track tile coordinates.
- Track adjacency.
- Expose current tile lookup.
- Provide debug data.

### `ChunkStreamingManager`

Owns 3D chunk instances.

Initial duties:

- Convert tile coordinates to world positions.
- Spawn matching placeholder 3D hex chunks.
- Keep chunks aligned.
- Later, stream around player instead of spawning all chunks.

### `PlayerStatsManager`

Owns stamina/chill values.

Initial duties:

- Track stamina.
- Track chill.
- Apply sprint drain.
- Apply recovery.
- Expose values for debug UI.

### `RuneManager`

Owns rune lifecycle.

Initial duties:

- Select rune tile.
- Spawn rune placeholder.
- Track collected rune.
- Accept deposit at standing stones.

### `PursuerManager`

Owns threat pressure.

Initial duties:

- Track simple pressure state.
- Escalate after rune pickup.
- React to sprint/noise later.
- Expose state in debug UI.

### `DebugManager`

Owns developer overlays and commands.

Initial duties:

- Toggle tile IDs.
- Toggle chunk boundaries.
- Show current tile.
- Show stamina/chill.
- Show rune state.
- Show pursuer state.

## First Data Objects

### `TileDefinition`

Suggested fields:

- Tile ID.
- Display/debug name.
- Chunk prefab.
- Terrain type.
- Elevation band.
- Edge connection profile.
- Landmark slots.
- Rune eligible flag.
- Danger value.
- Chill modifier.
- Stamina modifier.

### `TileInstance`

Suggested runtime fields:

- Tile ID.
- Axial/grid coordinate.
- World position.
- Rotation.
- Neighbor coordinates.
- Spawned chunk reference.
- Runtime flags.

### `TileLayoutDefinition`

Suggested fields:

- Seed.
- Layout radius.
- Starting tile rules.
- Home tile coordinate.
- Required tile IDs.
- Random tile pool.
- Adjacency constraints.

### `PlayerStatsTuning`

Suggested fields:

- Max stamina.
- Sprint drain rate.
- Walk recovery rate.
- Exhausted recovery delay.
- Base chill gain rate.
- Chill danger thresholds.
- Slope stamina multiplier.

### `PursuerTuning`

Suggested fields:

- Dormant duration.
- Search pressure rate.
- Stalk pressure rate.
- Close pressure distance.
- Rune pickup escalation.
- Sprint noise modifier.

## First Prototype Build Order

1. Create Unity project and folder structure.
2. Create one empty prototype scene.
3. Implement `TileDefinition` and `TileInstance`.
4. Implement `TileLayoutManager` with a fixed small hex layout.
5. Implement `ChunkStreamingManager` with placeholder hex chunks.
6. Add standing stones on the home tile.
7. Add first-person controller.
8. Add current-tile tracking.
9. Add stamina/chill.
10. Add one rune spawn and deposit loop.
11. Add debug overlay.
12. Add primitive pursuer pressure.

## First Test Layout

Use a small radius layout before randomization:

- 1 center home tile.
- 6 adjacent ring tiles.
- 12 outer ring tiles.

Total: 19 tiles.

First test goals:

- Confirm tile placement works.
- Confirm chunk seams are acceptable.
- Confirm player current tile detection works.
- Confirm rune can spawn away from home.
- Confirm the player can return by reading landmarks.

Random tile draw should come after the fixed layout works.

## What Not To Build Yet

- Full procedural world generation.
- Final terrain art.
- Final tree system.
- Final pursuer model.
- Complex AI navigation.
- Full UI.
- Save/load.
- Complex inventory.
- Multiple biomes.
- Final audio pass.

## Immediate Decision

The first implementation thread should be:

**Phase 2 Tile Layout and Chunk Architecture**

Deliverable:

A Unity scene where hidden tile IDs drive visible 3D hex chunk placement, with debug labels/boundaries and a current-tile readout.

Detailed construction spec:

- [Phase2_Board_Tile_Construction_Spec](Phase2_Board_Tile_Construction_Spec.md)
