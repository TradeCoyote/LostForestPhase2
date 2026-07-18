# Phase 2 Unity Bootstrap Status

## Current State

The repository has been prepared as a lightweight Unity project shell for Lost Forest Phase 2.

Unity editor detected:

- Unity 6000.5.0f1
- Path: `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app`

## Bootstrap Completed

- Added Unity `.gitignore`.
- Added `Packages/manifest.json` with no extra dependencies.
- Added `ProjectSettings/ProjectVersion.txt`.
- Added `ProjectSettings/EditorBuildSettings.asset`.
- Established planned `Assets/LostForest` folder structure.
- Preserved current Phase 2 world scripts under `Assets/LostForest/Scripts/World`.
- Added stable `.meta` files for current world scripts.
- Created prototype scene:
  - `Assets/LostForest/Scenes/Phase2_HiddenFieldTest.unity`
- Added a scene GameObject named `Field Generation Debug Runner`.
- Attached `FieldGenerationDebugRunner`.
- Configured the runner to generate the canonical 26 x 26 Field on start with seed `12345`.

## Intended First Scene Behavior

When the scene is opened and played in Unity:

1. `FieldGenerationDebugRunner.Start()` calls `Generate()`.
2. `FieldGenerator.Generate()` creates the hidden Field.
3. The Field contains 26 rows and 26 columns.
4. The Field contains 676 Slots.
5. Tile `000` is assigned to the player/home spawn Slot.
6. Tile `666` is assigned to the pursuer spawn Slot.
7. Other Tiles are uniquely drawn from the 000-999 Tile bank.
8. Orientation index is assigned from 0-5.
9. Unity Console logs `Lost Forest Phase 2 Field Slot Report`.

## Verification

Manual Unity verification succeeded after opening the correct project and scene through Unity Hub.

Observed Console messages:

- `Lost Forest Phase 2 FieldGenerationDebugRunner is active.`
- `Lost Forest Phase 2 Field Slot Report`

Observed Field report values:

- Rows: `26`
- Columns: `26`
- Slots Filled: `676`
- Tiles Remaining: `324`
- Seed: `12345`
- Player/home Tile `000`: Slot `J10`
- Pursuer Tile `666`: Slot `F1`

Unity batch validation did not reach compilation earlier because the editor could not complete licensing startup from the terminal.

Observed log line:

`Licensing initialization failed`

This appears to be a Unity licensing/session startup issue, not a reported compile error. Unity did not reach project import or script compilation in batch mode.

## Next Manual Check

Open the project through Unity Hub, using Unity 6000.5.0f1.

Then:

1. Open `Assets/LostForest/Scenes/Phase2_HiddenFieldTest.unity`, or use the top menu command `Lost Forest > Bootstrap > Open Hidden Field Test Scene`.
2. Press Play.
3. Confirm the Console logs `Lost Forest Phase 2 Field Slot Report`.
4. Confirm there is no visible board-game presentation.
5. Report any compile errors before expanding scope.

Expected Console messages:

- `Lost Forest Phase 2 FieldGenerationDebugRunner is active.`
- `Lost Forest Phase 2 Field generated. Rows=26, Columns=26, Slots=676, Seed=12345`
- `Lost Forest Phase 2 Field Slot Report`

If no messages appear, check that the Console info/log filter is enabled and that the `Field Generation Debug Runner` GameObject does not show a missing script.

If the Hierarchy only shows a camera and a light, Unity is showing a default scene rather than the hidden Field test scene. Use `Lost Forest > Bootstrap > Open Hidden Field Test Scene`. If needed, use `Lost Forest > Bootstrap > Create or Repair Hidden Field Test Scene`.

## 7 Hex Terrain Frame Test

Added a dedicated terrain-frame debug test for the next construction milestone.

Source:

- `Assets/LostForest/Scripts/World/SevenHexTerrainFrameDebugView.cs`
- `Assets/LostForest/Scripts/World/HexTerrainMeshBuilder.cs`
- `Assets/LostForest/Scripts/World/HexTerrainCollisionBuilder.cs`
- `Assets/LostForest/Scripts/World/HexTerrainMeshData.cs`
- `Assets/LostForest/Scripts/World/HexTerrainMeshSettings.cs`
- `Assets/LostForest/Scripts/World/TerrainFrameData.cs`
- `Assets/LostForest/Scripts/World/TerrainSlotData.cs`
- `Assets/LostForest/Scripts/World/TerrainFrameSettings.cs`
- `Assets/LostForest/Scripts/World/TerrainFrameGenerator.cs`
- `Assets/LostForest/Scripts/World/SharedHeightPoint.cs`
- `Assets/LostForest/Scripts/World/TerrainPointKind.cs`
- `Assets/LostForest/Scripts/Debug/DebugOrbitCamera.cs`
- `Assets/LostForest/Scripts/Editor/SevenHexTerrainFrameTestSceneBootstrap.cs`

Shared terrain data extraction:

- `TerrainFrameGenerator.GenerateRadiusOne(...)` now builds the reusable radius-1 Frame terrain graph.
- `TerrainFrameData` owns the shared height-point lookup, stable point IDs, point counts, local reference counts, and shared-boundary validation.
- `TerrainSlotData` stores each Slot's axial coordinate, neighbor indices, and local references, such as `Center.V0`, `East.E3`, and `Northwest.I5`, to Frame-owned shared points.
- `TerrainFrameSettings` carries the deterministic prototype height settings used by the generator.
- `SevenHexTerrainFrameDebugView` now consumes the generated data graph for point markers, labels, wireframe, report output, and the orange Tile conformity proof.
- `HexTerrainMeshBuilder` now builds the white low-poly terrain surfaces from `TerrainSlotData` and `SharedHeightPoint` references instead of keeping mesh topology inside the debug view.
- `HexTerrainCollisionBuilder` owns built-in Physics `MeshCollider` creation for terrain surfaces and assigns the same generated mesh used by the visible terrain.
- `HexTerrainMeshSettings` carries extension handles for surface lift, mesh naming, group naming, optional per-surface colliders, static marking, and generation reporting.
- `HexTerrainMeshData` reports generated surface count, mesh count, vertex count, triangle count, mesh collider count, skipped mesh count, skipped collider count, and collider readiness for one Slot, a small group, or a future larger chunk.
- Tile conformity remains read-only: rotated Tile anchors look up Frame-owned height points by deterministic world-position point ID and do not author terrain height.
- Default radius-1 generation should report 7 Slots, 133 local point references, 103 unique shared points, 30 reused point references, and shared-boundary validation `Passed`.

Purpose:

- Generate a radius-1 Frame: 1 center hex plus 6 adjacent hexes.
- Use a 100 meter flat-to-flat hex size.
- Draw visible debug grid lines.
- Show center points.
- Show vertex points.
- Show edge midpoint points.
- Show inner ring points.
- Deduplicate points by world position so shared boundary/intersection points have one global height-point ID.
- Assign deterministic Frame-owned height values to shared points.
- Move point markers and wire lines to their generated height values.
- Report local point references, unique shared points, reused point references, kind counts, and a sample shared point.
- Report that terrain meshes were generated by `HexTerrainMeshBuilder`.
- Add a development orbit camera for inspecting the height frame in 3D.
- Add a Tile conformity proof where rotated Tile anchors read Frame-owned height points and display as orange debug cubes.
- Generate seven simple white triangulated terrain surfaces from the Frame-owned height points.
- Add optional `MeshCollider` components to generated terrain surfaces for the later walk test when Unity's built-in Physics module is enabled.
- Keep terrain collider generation inspector-configurable through the 7-hex debug view and reusable through `HexTerrainMeshSettings`.
- Lift debug wire lines slightly above the terrain surface so construction remains readable.
- Configure the terrain frame test light as a stronger three-quarter directional key light.
- Keep tile rotation separate from Frame/Slot/point orientation.

Manual verification:

1. Open the project through Unity Hub.
2. Use `Lost Forest > Bootstrap > Create or Repair 7 Hex Terrain Frame Test Scene`.
3. Confirm Unity creates or opens `Assets/LostForest/Scenes/Phase2_SevenHexTerrainFrameTest.unity`.
4. Confirm the scene contains `7 Hex Terrain Frame Debug View`.
5. Confirm the Scene view shows 7 large visible hexes.
6. Confirm the center labels appear.
7. Confirm colored point spheres show centers, vertices, edge midpoints, and inner points.
8. Confirm the Console logs a message like `7 hex terrain frame rebuilt`.
9. Confirm the Console logs `Lost Forest Phase 2 Frame Height Point Debug`.
10. Confirm the report includes:
   - `Slots`
   - `Local point references`
   - `Unique shared points`
   - `Reused point references`
   - `Multi-reference shared points`
   - `Shared-boundary validation`
   - kind counts for centers, vertices, edge midpoints, and inner points
   - a sample shared point with multiple local references
   - `Terrain mesh builder`
   - `Terrain meshes generated`
   - `Terrain mesh collider generation`
   - `Terrain mesh colliders`
   - `Terrain meshes skipped`
   - `Terrain mesh colliders skipped`
   - `Terrain collider validation`
11. Press Play and confirm the camera orbits the 7-hex height frame in perspective.
12. Confirm orange debug cubes appear on the center hex as a dropped/rotated Tile anchor conformity proof.
13. Confirm seven white terrain surfaces appear under the debug wireframe.
14. Confirm the 3/4 directional light makes height variation readable.
15. Confirm the Console report includes:
   - `Tile conformity proof`
   - `Conforming tile orientation`
   - `Terrain surface`
16. Report any compile errors before expanding into player/collision testing.

## Terrain Collider Readiness

The generated 7-hex terrain surfaces now use optional per-surface `MeshCollider` components through `HexTerrainCollisionBuilder`.

To test collision readiness:

1. Open `Assets/LostForest/Scenes/Phase2_SevenHexTerrainFrameTest.unity`, or run `Lost Forest > Bootstrap > Create or Repair 7 Hex Terrain Frame Test Scene`.
2. Select `7 Hex Terrain Frame Debug View`.
3. In the Terrain Surface inspector group, enable `Add Terrain Mesh Colliders`.
4. Run the context menu command `Rebuild 7 Hex Terrain Frame`.
5. Confirm the Console report includes `Terrain mesh collider generation` as `Enabled`, `Terrain mesh colliders` as `7`, `Terrain meshes skipped` as `0`, `Terrain mesh colliders skipped` as `0`, and `Terrain collider validation` as `OK`.
6. Disable `Add Terrain Mesh Colliders`, rebuild again, and confirm the report shows collider generation disabled and zero generated terrain mesh colliders.
7. The context menu command `Validate Terrain Mesh Colliders` can be used after a rebuild to re-log collider readiness without adding a player controller.

This is still terrain/frame infrastructure only. It does not add player movement, visible board collision, Tile-authored collision, or tile-rotation-driven terrain behavior.

## Early WalkThru Test

Added the first minimal first-person walk-test path for proving terrain/collider traversal over the generated 7-hex terrain slice.

Source:

- `Assets/LostForest/Scripts/Player/EarlyWalkThruFirstPersonController.cs`
- `Assets/LostForest/Scripts/Player/EarlyWalkThruCenterSpawn.cs`
- `Assets/LostForest/Scripts/Player/EarlyWalkThruPositionLogger.cs`
- `Assets/LostForest/Scripts/Editor/EarlyWalkThruTestSceneBootstrap.cs`
- `Assets/LostForest/Scripts/Player/PlayerTerrainRegionTracker.cs`
- `Assets/LostForest/Scripts/World/HomeRegionDefinition.cs`
- `Assets/LostForest/Scripts/Landmarks/HomeLandmarkBuilder.cs`

Use `Lost Forest > Bootstrap > Create or Repair Early WalkThru Test Scene` to create or repair:

- `Assets/LostForest/Scenes/Phase2_EarlyWalkThruTest.unity`

Default behavior:

- Reuses the Frame-owned 7-hex terrain generation stack.
- Keeps terrain MeshColliders enabled.
- Spawns the player just above a selected Slot center point, defaulting to the center Slot.
- Does not spawn on shared or unshared hex edges.
- Uses keyboard WASD movement and mouse look only.
- Tracks the player's current hidden Terrain Slot from generated `TerrainFrameData`.
- Defines the Home Region as the center Slot, axial `(0, 0)`, by default.
- Places a simple three-stone Home landmark at the Home Slot center.
- Uses black primitive cylinders with ordered child names and varied height, scale, and rotation so Home is visible in first-person.
- Logs current player XYZ position to the Unity Console, including Slot/Home state when the tracker is present.
- Logs Home landmark placement success, Home Slot label, axial coordinate, and world anchor position.
- Keeps hex points and construction lines visible for this test.
- Hides center labels, point labels, and the orange Tile conformity proof by default.
- Adds no runes, stamina, chill, pursuer behavior, final art, or player-facing board UI.

Manual verification:

1. Use `Lost Forest > Bootstrap > Create or Repair Early WalkThru Test Scene`.
2. Confirm the scene contains `Home Standing Stone Landmark` with three child cylinder primitives.
3. Confirm the black cylinders appear at the center Home Region and read as an in-world snowy-forest landmark, not board UI.
4. Confirm the Console logs `Lost Forest Home Landmark placement` with `Succeeded=True`, Slot label, axial `(0, 0)`, and world position.
5. Press Play.
6. Confirm the Console logs the spawn XYZ, the starting player region, and recurring player XYZ positions.
7. Move with WASD and look with the mouse.
8. Walk around the standing stones, then move from the center hex toward neighboring hexes and test seam crossings.
9. Confirm the Console logs region transitions only when the nearest generated Terrain Slot changes.
10. Confirm transition logs identify current Slot label, axial coordinate, center world position, previous Slot, and `IsHome`.
11. Return to the center Slot and confirm `IsHome=True`.
12. Confirm the player lands on terrain collision instead of falling through.

## Player Current Region / Home Region

Added the first hidden gameplay identity bridge for the walkable 7-hex terrain proof.

How it works:

- `PlayerTerrainRegionTracker` lives on the first-person player.
- It references the active `SevenHexTerrainFrameDebugView` for this prototype, then reads its generated `TerrainFrameData`.
- If the frame has not generated data yet, the tracker can ask the terrain view to rebuild before querying.
- Current region is resolved by nearest generated `TerrainSlotData` center using horizontal X/Z distance.
- The tracker exposes the current Slot, previous Slot, label, axial coordinate, center world position, current frame data, the Home Region definition, and `IsInHomeRegion`.
- Region logs are emitted when the player enters a different nearest Slot, with an initial log on Play.

Home Region:

- `HomeRegionDefinition` is a lightweight data holder on the terrain frame object in the Early WalkThru scene.
- The bootstrap sets Home to axial `(0, 0)`, matching the center Terrain Slot in the radius-1 frame.
- The label fallback is `Center` so the 7-hex proof remains robust while the frame data stays small.

Home Landmark:

- `HomeLandmarkBuilder` is a reusable scene component that resolves placement through `HomeRegionDefinition.TryGetHomeSlot(...)`.
- The placement anchor is the resolved Home Slot center point from Frame-owned terrain data.
- The current placeholder creates three black cylinders offset slightly forward from the anchor so they are visible from the first-person spawn.
- A future stone prefab can be assigned on the builder without changing the Home Slot placement path.
- Future Tile `000` or full-Field Home selection can update `HomeRegionDefinition`; the landmark builder should not need to change.
- Future rune-return logic can query `HomeLandmarkBuilder.HomeSlot`, `AnchorWorldPosition`, `HasPlacement`, or `TryGetHomeAnchorWorldPosition(...)`.

Expansion notes:

- A future `TerrainFrameManager` or `WorldManager` should provide the same `TerrainFrameData` shape now provided by `SevenHexTerrainFrameDebugView`.
- Radius-2 / 19-tile expansion should only require the provider to generate more Slots; nearest-center tracking loops over `TerrainFrameData.Slots`.
- The Home landmark placement also loops through `TerrainFrameData` via the Home Region definition, so radius-2 / 19-tile expansion should not require a separate placement path.
- Exact hex containment can replace the nearest-center method inside `PlayerTerrainRegionTracker` without changing downstream callers.
- Future terrain tags, danger, visibility, chill modifiers, rune eligibility, and pursuer pressure can hang off the current Slot query path.

## Scope Held Back

The bootstrap intentionally does not include:

- Final player movement.
- 3D chunks.
- Full 26 x 26 runtime terrain generation.
- Runes.
- Pursuer behavior.
- Visible board presentation.
- Player-facing grid or tile labels.
- Final landmark art.
