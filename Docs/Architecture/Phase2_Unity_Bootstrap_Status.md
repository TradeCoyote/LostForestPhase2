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
- `Assets/LostForest/Scripts/World/SharedHeightPoint.cs`
- `Assets/LostForest/Scripts/World/TerrainPointKind.cs`
- `Assets/LostForest/Scripts/Debug/DebugOrbitCamera.cs`
- `Assets/LostForest/Scripts/Editor/SevenHexTerrainFrameTestSceneBootstrap.cs`

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
- Add a development orbit camera for inspecting the height frame in 3D.
- Add a Tile conformity proof where rotated Tile anchors read Frame-owned height points and display as orange debug cubes.
- Generate seven simple white triangulated terrain surfaces from the Frame-owned height points.
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
   - `Local point references`
   - `Unique shared points`
   - `Reused point references`
   - `Multi-reference shared points`
   - kind counts for centers, vertices, edge midpoints, and inner points
   - a sample shared point with multiple local references
11. Press Play and confirm the camera orbits the 7-hex height frame in perspective.
12. Confirm orange debug cubes appear on the center hex as a dropped/rotated Tile anchor conformity proof.
13. Confirm seven white terrain surfaces appear under the debug wireframe.
14. Confirm the 3/4 directional light makes height variation readable.
15. Confirm the Console report includes:
   - `Tile conformity proof`
   - `Conforming tile orientation`
   - `Terrain surface`
16. Report any compile errors before expanding into player/collision testing.

## Scope Held Back

The bootstrap intentionally does not include:

- Player movement.
- 3D chunks.
- Terrain.
- Runes.
- Pursuer behavior.
- Visible board presentation.
- Player-facing grid or tile labels.
