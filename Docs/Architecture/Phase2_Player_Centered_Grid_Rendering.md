# Phase 2 Player-Centered Grid Rendering Prototype

## Purpose

This prototype moves Lost Forest Phase 2 from the fixed radius-1 terrain proof toward a player-centered renderer that follows the hidden game board.

The player sees first-person snowy forest. The simulation still resolves movement through the generated `26 x 26` Field, with Frame Slots, Grid Addresses, Tile IDs, orientations, roles, and axial distance remaining authoritative.

## How To Run

Use the Unity menu command:

`Lost Forest > Bootstrap > Create or Repair Grid Movement Fog Test Scene`

This creates or repairs:

`Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity`

Press Play in that scene. The runtime manager generates the canonical hidden Field, finds the Home Slot / Tile `000`, renders the active slots around Home, places the first-person player on the Home terrain surface, and shows the temporary grid debug HUD.

## Active Render Radius

`ActiveRegionRenderer` owns the render window.

Current default:

- Radius `0`: current hidden Slot.
- Radius `1`: the six neighboring hidden Slots, for up to seven rendered Slots total.
- Outside the radius: removed immediately.

The renderer already stores a distance band on each `RenderedSlotInstance`, so radius `2`, low-detail silhouettes, fade-out, and fog boundary behavior can attach without changing how player Slot identity is resolved.

## Hidden Board Preservation

`GridMovementWorldManager` generates a full `FieldData` through `FieldGenerator.Generate(...)`.

`PlayerGridAddressTracker` resolves the player's world position back to the nearest hidden `FieldSlotData`. When the player crosses a Slot boundary, it logs the transition and the manager re-centers the active render window.

Each rendered Slot is generated from its hidden Field Slot:

- Grid Address.
- Row and column.
- Axial coordinate.
- Tile ID.
- Tile orientation.
- Slot role.

Terrain is generated at the Field Slot's canonical world center. Placeholder Tile content is spawned from the Tile definition and receives the stored Tile orientation. Home stones are spawned only when the rendered Slot is the Home Slot.

## Current Source

- `Assets/LostForest/Scripts/World/GridMovementWorldManager.cs`
- `Assets/LostForest/Scripts/World/ActiveRegionRenderer.cs`
- `Assets/LostForest/Scripts/World/RenderedSlotInstance.cs`
- `Assets/LostForest/Scripts/Player/PlayerGridAddressTracker.cs`
- `Assets/LostForest/Scripts/Debug/GridDebugHud.cs`
- `Assets/LostForest/Scripts/Landmarks/HomeStoneLandmarkRenderer.cs`
- `Assets/LostForest/Scripts/Editor/GridMovementFogTestSceneBootstrap.cs`

## Next

- Increase active radius to `2` and use `RenderedSlotInstance.DistanceFromCenter` for detail bands.
- Add a fade or pool path behind `ActiveRegionRenderer.RemoveRenderedSlot(...)`.
- Attach fog and silhouette rendering to distance bands.
- Add board-layer state stores for visited Slots, rune locations, danger pressure, and future pursuer position.
- Keep hunter/pursuer behavior out until the board-layer movement proof is stable.
