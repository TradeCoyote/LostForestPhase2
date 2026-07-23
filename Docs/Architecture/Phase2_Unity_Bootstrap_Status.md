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
- `Assets/LostForest/Scripts/Feedback/PrototypeBirchForestDebugSpawner.cs`
- `Assets/LostForest/Scripts/Feedback/PrototypeFogDirector.cs`
- `Assets/LostForest/Scripts/Editor/EarlyWalkThruTestSceneBootstrap.cs`

Use `Lost Forest > Bootstrap > Create or Repair Early WalkThru Test Scene` to create or repair:

- `Assets/LostForest/Scenes/Phase2_EarlyWalkThruTest.unity`

Default behavior:

- Reuses the Frame-owned 7-hex terrain generation stack.
- Keeps terrain MeshColliders enabled.
- Spawns the player just above a selected Slot center point, defaulting to the center Slot.
- Does not spawn on shared or unshared hex edges.
- Uses keyboard WASD movement and mouse look only.
- Logs current player XYZ position to the Unity Console.
- Keeps hex points and construction lines visible for this test.
- Hides center labels, point labels, and the orange Tile conformity proof by default.
- Adds a prototype birch fog-readability spawner to the scene.
- The spawner generates tall pale trunk stand-ins at Play time, using a `3x` trunk height multiplier over the earlier prototype range.
- Each trunk gets a fixed reusable pattern of small dark circular bark bands at varied heights so fog tests can verify that markings are unclear in thick fog and become clearer as fog thins or the player approaches.
- Adds a cheap prototype distance fog director using Unity built-in fog for first-pass field-of-view testing.
- Adds no runes, stamina, chill, pursuer behavior, landmarks, or player-facing board UI.

This scene is now a fixed 7-cell terrain and movement lab. It is not the current player-centered Field travel test.

Manual verification:

1. Use `Lost Forest > Bootstrap > Create or Repair Early WalkThru Test Scene`.
2. Press Play.
3. Confirm the Console logs the spawn XYZ and recurring player XYZ positions.
4. Move with WASD and look with the mouse.
5. Walk from the center hex toward neighboring hexes and test seam crossings.
6. Confirm the player lands on terrain collision instead of falling through.

## Grid Movement Fog Test

Added the player-centered hidden Field travel test.

Source:

- `Assets/LostForest/Scripts/World/GridMovementWorldManager.cs`
- `Assets/LostForest/Scripts/World/ActiveRegionRenderer.cs`
- `Assets/LostForest/Scripts/World/RenderedSlotInstance.cs`
- `Assets/LostForest/Scripts/Player/PlayerGridAddressTracker.cs`
- `Assets/LostForest/Scripts/Debug/GridDebugHud.cs`
- `Assets/LostForest/Scripts/Editor/GridMovementFogTestSceneBootstrap.cs`

Use `Lost Forest > Bootstrap > Create or Repair Grid Movement Fog Test Scene` to create or repair:

- `Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity`

Default behavior:

- Generates the canonical hidden `26 x 26` Field.
- Finds Home / Tile `000`.
- Spawns the player at Home.
- Tracks the player's current hidden Slot while walking.
- Renders an active radius around the player's current Slot.
- Re-centers rendered Slots when the player crosses into a new hidden Slot.
- Uses cheap built-in distance fog through `PrototypeFogDirector`.
- Spawns reduced-count tall pale birch stand-ins with fixed dark bark bands for fog readability.

This is the scene to use when testing travel through the hidden Field. The older Early WalkThru scene remains useful only for the fixed 7-cell terrain/collider lab.

## Scope Held Back

The bootstrap intentionally does not include:

- Final player movement.
- 3D chunks.
- Full 26 x 26 runtime terrain generation.
- Runes.
- Pursuer behavior.
- Visible board presentation.
- Player-facing grid or tile labels.
