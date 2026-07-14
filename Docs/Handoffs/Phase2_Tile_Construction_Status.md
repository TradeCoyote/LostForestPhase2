# Phase 2 Tile Construction Status

## Added

- A lightweight `TileDefinition` model for static tile identity, debug name, reserved role, rune eligibility placeholder, terrain/content tags, rotation support, and construction anchors.
- A `TileInstance` model for one dropped tile assignment. It references the tile ID/definition, the assigned Slot address, row/column, axial coordinate, orientation index/degrees, and the Slot-provided world position.
- A `TileConstructionAnchors` model with center, six edge, six corner, six inner, and optional prop/content anchors. These anchors are authored in tile-local space before placement.
- A small `TileDefinitionRegistry` with explicit prototype definitions for Tile `000`, Tile `666`, and lazy fallback definitions for ordinary field Tiles.
- A development-only `TileConstructionDebugRunner` that drops prototype Tile `000` into one generated Slot and shows the contract visually.

## How To Test

1. In Unity, choose `Lost Forest > Bootstrap > Create or Repair Tile Construction Test Scene`.
2. Open `Assets/LostForest/Scenes/Phase2_TileConstructionTest.unity`.
3. Select `Tile Construction Debug Runner`.
4. Change `Orientation Index` between `0` and `5`, then use the component context menu `Rebuild Tile Construction Debug`.
5. Optionally set `Test Tile Id` to `666` or another field Tile ID to check registry lookup/fallback behavior.

Expected result:

- Blue markers are Frame-owned terrain anchors. They stay fixed for the Slot.
- Orange and red markers are Tile content anchors. They rotate in 60-degree steps around the Slot center.
- The debug log reports the Slot address, axial coordinate, world position, Tile ID, and orientation.
- The debug log says the definition came from `TileDefinitionRegistry`.

## Next

- Replace the prototype registry with authored tile-definition assets when the tile bank needs designer-owned data.
- Use the same `TileInstance` assignment boundary when chunk prefabs are introduced.
- Keep shared terrain height/stitching data on the Frame/Slot side, not inside the Tile.
