# Hex and Board Construction Thread - Phase 1 Pass-On Document

## 1. System Purpose

This system established the foundational board logic for Phase 1 of Lost Forest, the 2D board-game prototype.

Its purpose was to define and generate the underlying game board, including:

- The `26 x 26` hexagonal board structure known as The Frame.
- Individual addressable board spaces known as Slots.
- A bank of 1000 uniquely identified Tiles.
- Randomized Field generation, where Tiles are placed into Slots.
- Tile orientation tracking.
- Special spawn placement for the player and pursuer.
- Debug selection, visualization, and logging tools.

This system was not responsible for final art, movement rules, combat, inventory, full win/loss logic, tile type behavior, or environmental storytelling. It was the structural prototype layer: the mathematical and organizational skeleton of the game world.

Phase 1 proved that Lost Forest can be represented as a deterministic-addressable, randomly populated hex Field, and that this Field can produce useful data for future systems.

## 2. What We Built

### Core Board Structure

We built a Unity runtime system that generates a flat-topped hexagonal board.

The board is:

- 26 rows by 26 columns.
- 676 total Slots.
- Rows labeled `A-Z`.
- Columns labeled `1-26`.
- Every Slot has a unique address such as `A1`, `H9`, `M14`, `X22`, or `Z26`.

This empty board structure is called The Frame.

Once all Slots are filled with Tiles, the result is called The Field.

### Slots

Each Slot stores:

- Address.
- Row index.
- Column index.
- Tile ID.
- Tile orientation index.
- Orientation in degrees.

Example:

```text
Address: M14
Tile: 000
OrientationIndex: 3
OrientationDegrees: 180
```

Internally, rows and columns use zero-based indexing:

- `A1 = row 0, column 0`
- `M14 = row 12, column 13`
- `Z26 = row 25, column 25`

### Tile Bank

We created a bank of 1000 Tiles.

Tile IDs run from `000` through `999`.

Each new Field draws 676 unique Tiles from the bank:

- 676 Tiles placed.
- 324 Tiles remain unused.

At this stage, Tile IDs are debug/reference values rather than player-facing names.

### Tile Orientation

Each Tile can be placed in one of six orientations:

- `0 = 0 degrees`
- `1 = 60 degrees`
- `2 = 120 degrees`
- `3 = 180 degrees`
- `4 = 240 degrees`
- `5 = 300 degrees`

Orientation is randomized per placed Tile.

This matters because future tile types may depend on facing, path openings, hazards, line of sight, or environmental connections.

### Field Generation

The current generation process:

1. Clear any existing generated Field.
2. Prepare a Tile bank containing IDs `000-999`.
3. Reserve Tile `000` for the player spawn.
4. Reserve Tile `666` for the pursuer spawn.
5. Place Tile `000` in the middle region.
6. Place Tile `666` in the outer lanes.
7. Fill all remaining Slots randomly with unique Tiles.
8. Assign each Tile a random orientation.
9. Generate runtime hex meshes.
10. Generate labels, row/column markers, colliders, and debug data.
11. Generate a full Field Slot Report.

### Special Spawn Tiles

Tile `000` is the Player Spawn Tile.

- Placed before the rest of the board fills.
- Randomly placed in the middle of the board.
- Current range: rows `J-Q`, columns `10-17`.
- Colored green for debug visibility.

Tile `666` is the Pursuer Spawn Tile.

- Placed before the rest of the board fills.
- Randomly placed in the outer three lanes.
- Outer lanes mean rows `A-C`, rows `X-Z`, columns `1-3`, or columns `24-26`.
- Colored red for debug visibility.

### Debug Visuals

The generated Field currently includes:

- Slot address labels.
- Tile ID/orientation labels.
- Outer row markers.
- Outer column markers.
- Green player spawn Tile.
- Red pursuer spawn Tile.

These are debug aids and should not be considered final player-facing UI.

### Click Selection

Each generated hex Tile has a collider matching the hex shape.

Clicking a Tile:

- Highlights the selected Tile.
- Updates a small debug display under the Start button.
- Shows Slot address, Tile ID, and orientation.

Example:

```text
Selected Slot
Address: M14
Tile: 000
Orientation: 3 (180 deg)
```

### Debug Field Report

A full Field Slot Report was added.

The report lists every Slot in the Field and includes:

- Slot address.
- Tile ID.
- Orientation index.
- Orientation degrees.
- Role.

Current role values:

- `PlayerSpawn`
- `PursuerSpawn`
- `Field`

Example row:

```text
M14    000    3    180    PlayerSpawn
```

The report was intentionally made flexible so future data can be added, such as:

- Tile type.
- Terrain type.
- Biome.
- Encounter data.
- Movement cost.
- Visibility state.
- Item contents.
- Event flags.
- Narrative state.
- 3D prefab mapping.

### Unity Scripts

Current runtime scripts:

- `FieldSlot.cs`
- `HexAndFieldManager.cs`
- `TheGameRunner.cs`
- `SlotClickTarget.cs`
- `LostForestBootstrap.cs`
- `FieldSlotLogColumn.cs`

Responsibilities:

- `FieldSlot.cs`: stores Slot data once a Field is generated, including address, row, column, Tile ID, and orientation. Exposes formatted Tile ID and orientation degrees.
- `HexAndFieldManager.cs`: builds The Frame, prepares the Tile bank, reserves spawn Tiles, fills The Field, creates hex mesh objects, creates labels and markers, creates colliders, colors spawn Tiles, and builds the Field Slot Report.
- `TheGameRunner.cs`: owns the start flow, creates a Start button if one is missing, calls the Field builder, frames the camera, handles mouse selection, updates selected Slot display, and contains placeholder win/loss hooks.
- `SlotClickTarget.cs`: attached to generated Tiles, stores the Slot reference, and handles selected/unselected visual state.
- `LostForestBootstrap.cs`: automatically creates prototype manager GameObjects if none exist, allowing a fresh scene to run the prototype without manual setup.
- `FieldSlotLogColumn.cs`: allows the Field Slot Report to gain additional columns later.

## 3. Current Phase 1 Rules

### Confirmed Rules

- The board is called The Frame before Tiles are placed.
- The generated board is called The Field after Tiles are placed.
- The Frame is `26 x 26`.
- Rows are letters `A-Z`.
- Columns are numbers `1-26`.
- Each addressable space is called a Slot.
- Each Slot receives one Tile.
- The Tile bank contains 1000 Tiles.
- Tile IDs are `000-999`.
- A generated Field uses 676 unique Tiles.
- 324 Tiles remain unused after Field generation.
- Each Tile has one of six orientations.
- Tile orientation is tracked as both index and degrees.
- Tile `000` is the player spawn.
- Tile `666` is the pursuer spawn.
- Tile `000` is placed in the middle region.
- Tile `666` is placed in the outer three lanes.
- Spawn Tiles are placed before the rest of the board is filled.
- Spawn Tiles are removed from the random Tile bank before normal random fill.
- Tile `000` is green in debug view.
- Tile `666` is red in debug view.
- Each Slot can be clicked and inspected.
- A full Field Slot Report is generated for debugging.

### Experimental or Temporary Rules

- All non-spawn Tiles are visually identical.
- Tile IDs are not player-facing.
- Debug labels are visible directly on Tiles.
- Start button and selected Slot display are temporary prototype UI.
- Player spawn region is currently rows `J-Q`, columns `10-17`.
- Pursuer spawn region is currently any Slot in the outer three lanes.
- Tile orientation has no gameplay effect yet.
- There are no tile types yet.
- There are no terrain types yet.
- There are no movement rules yet.
- There are no adjacency/path rules yet.
- There are no win/loss conditions beyond placeholder methods.
- There is no actual player pawn or pursuer pawn yet, only spawn Tiles.
- The camera auto-frames the entire board for 2D debugging.

## 4. What We Learned

### What Worked Well

The address system worked very well. The `A-Z` and `1-26` structure made it easy to discuss and inspect specific board locations. This should remain important under the hood in Phase 2 even if the player never sees board coordinates.

The board generated reliably. Repeated starts confirmed that Tile `000` appeared in the intended middle region, Tile `666` appeared in the intended outer region, the board filled completely, and Tile data remained inspectable.

The distinction between Frame, Slot, Tile, and Field became useful quickly:

- Frame = empty structure.
- Slot = addressable space.
- Tile = content placed into a Slot.
- Field = completed board state.

The click selection debug tool was valuable. Being able to click a Tile and inspect its Slot, Tile ID, and orientation made the procedural board feel tangible and debuggable.

The Field Slot Report is important. The full report is the bridge between procedural generation and designer trust. It lets the team verify what exists without relying only on visual inspection.

The spawn logic created immediate spatial tension. Even before gameplay movement exists, the player spawn being central and the pursuer spawn being peripheral establishes a useful spatial relationship:

- Player begins inside.
- Threat begins outside.
- The Field exists between them.

### What Created Tension

- The pursuer being tied to the outer lanes creates an implied pressure inward.
- The player spawn being near the middle implies uncertainty in all directions rather than a simple linear escape.
- The use of Tile `666` as pursuer spawn has immediate symbolic weight. It is not subtle, but it is strong for debug and early canon.

### What Caused Confusion

The phrase "rows and columns" can become ambiguous on a hex grid because hex boards do not behave exactly like square grids. For Phase 1, this was acceptable because the address system is abstract. For Phase 2, care will be needed when translating board coordinates into spatial terrain.

The board currently uses flat-topped hex layout, but Phase 2 may not need visible hexagons. The logical hex grid can remain under the hood while the player experiences natural terrain.

### What Proved Unnecessary for Now

- Tile types were intentionally postponed.
- Art assets were unnecessary for Phase 1 board proofing.
- Player-facing UI was unnecessary beyond debug tools.
- Win/loss conditions were unnecessary until movement and objectives exist.
- Complex pathing was unnecessary until terrain and tile meaning exist.

## 5. Important Design Decisions

### The Board Has a Formal Hidden Structure

Even if Phase 2 becomes a first-person exploration game, the world should still have an underlying structure.

The 2D Frame/Field system provides that structure. This matters because Phase 2 can use the board layer to control:

- World generation.
- Spawn logic.
- Pursuer distance.
- Encounter placement.
- Navigation logic.
- Environmental identity.
- Progression.
- Debugging.

### Slots Are Stable Addresses

Every generated space has a stable address.

This matters because designers can discuss the world with precision.

In Phase 2, these addresses may become invisible to the player but should remain available to debug tools.

### Tiles Are Unique

Each Tile ID is unique within a Field.

This matters because the system can eventually treat each Tile as a distinct content unit rather than just a terrain type.

A Tile can later carry:

- Mesh/prefab reference.
- Biome.
- Encounter.
- Path openings.
- Lore.
- Resource.
- Threat modifier.
- Audio bed.
- Lighting rule.

### Orientation Is Tracked From the Start

Even though orientation currently has no gameplay effect, it is already part of the data model.

This matters because Phase 2 tile chunks may need rotation. A forest clearing, path fork, cabin ruin, ravine, or landmark can be placed into a Slot and rotated consistently.

### Spawn Rules Are Spatial, Not Fixed

The player and pursuer do not spawn at exact fixed addresses. They spawn within regions.

This matters because each run can feel different while preserving the intended relationship:

- Player begins near the middle.
- Pursuer begins near the edge.

### Debuggability Was Prioritized

The board was built with labels, click inspection, colors, and reports.

This matters because procedural systems become hard to trust without clear inspection tools.

Phase 2 should keep strong debug tools even if they are hidden from the player.

## 6. Player Experience

In Phase 1, the player-facing experience is minimal because this is still a prototype.

However, the intended experience implied by the system is:

- The player begins somewhere inside a large unknown Field.
- The world has structure, but the player may not fully see it.
- A hostile presence begins somewhere distant, likely from the outer world.
- Every run has a different arrangement.
- The player's location matters.
- The pursuer's location matters.
- Direction and orientation may matter later.
- The world should feel generated but not arbitrary.

The player should eventually feel:

- Lost inside a structured but unknowable forest.
- Surrounded by a world that has rules.
- Pressured by something that begins outside their immediate space.
- Encouraged to read the environment.
- Unsure where the pursuer is, but aware that it exists.
- Dependent on landmarks, paths, sound, and memory rather than full-map certainty.

In Phase 1, the board is visible and inspectable.

In Phase 2, the player should not see the board directly. They should experience its consequences through terrain, paths, landmarks, and spatial tension.

## 7. Hidden System Logic

The board-game layer should remain important in Phase 2 even if it is hidden.

Important hidden logic:

### The Frame Still Exists

The `26 x 26` structure can remain as the invisible world-generation grid.

Each Slot can become a terrain cell, forest chunk, encounter zone, or streamed scene segment.

### Slot Addresses Still Matter

Addresses like `M14` should remain useful for:

- Debugging.
- Save data.
- Spawn tracking.
- Bug reports.
- Generation reports.
- Designer notes.
- Teleport tools.
- Heat maps.
- Pursuer state tracking.

The player does not need to see these addresses.

### Tile IDs Still Matter

Tile IDs can become content identifiers.

A Tile ID can eventually map to:

- A terrain prefab.
- A landmark type.
- A path configuration.
- A forest density profile.
- A soundscape.
- An event bundle.
- A danger level.
- A narrative fragment.

### Orientation Still Matters

Tile orientation should remain under the hood.

In 3D, orientation may rotate:

- Path entrances/exits.
- Terrain chunks.
- Landmark placement.
- Sightline blockers.
- Rivers or ravines.
- Ruins.
- Clearings.
- Pursuer routes.

### Spawn Rules Still Matter

The player spawn and pursuer spawn should remain region-based.

Player:

- Starts near the middle.
- Should feel surrounded.

Pursuer:

- Starts in the outer lanes.
- Should feel like it is entering, circling, hunting, or closing in.

### Full Field Report Still Matters

Phase 2 should keep an equivalent report.

The report should eventually include:

- Slot address.
- Tile ID.
- Orientation.
- Tile type.
- 3D prefab.
- Biome.
- Encounter state.
- Player visited/unvisited state.
- Pursuer path state.
- Audio zone.
- Lighting state.
- Navigation links.
- Runtime object references if useful.

## 8. Phase 2 Translation Notes

### What Should Remain Conceptually the Same

The following concepts should survive into Phase 2:

- The Frame as the hidden world grid.
- Slots as addressable world cells.
- Tiles as content assigned to Slots.
- The Field as the generated world state.
- Tile uniqueness.
- Tile orientation.
- Middle-region player spawn.
- Outer-lane pursuer spawn.
- Full debug reporting.
- Click/inspect equivalent, likely as editor or debug raycast tools.
- Region-based generation rather than fully fixed layouts.

### What Should Change Because the Game Becomes Spatial and First-Person

The board should no longer be presented as a visible tabletop grid.

The player should not see:

- Hex outlines.
- Slot labels.
- Tile IDs.
- Board coordinates.
- Full map layout by default.
- Debug color coding.

Instead, the player should experience the Field as a 3D forest.

A Slot may become:

- A terrain chunk.
- A clearing.
- A trail segment.
- A dense forest patch.
- A landmark zone.
- A cabin/ruin area.
- A hazard area.
- A transition area.

The hex grid can remain as the invisible placement framework.

### What Needs to Become Visual

The following debug concepts need visual equivalents:

- Tile type should become visible as environmental form: clearing, dense trees, trail, creek, hill, ravine, ruin, camp, landmark, or obstruction.
- Tile orientation should become visible through rotated spatial features: path direction, fallen logs, slope direction, opening direction, landmark facing, and trail exits.
- Spawn logic should become environmental introduction: player spawn should feel like waking or arriving in the interior. Pursuer spawn should not be obviously marked, but its entry should be supported by distant audio, environmental pressure, or later tracking.

### What Needs to Become Audio

The pursuer system especially should gain audio translation.

Possible audio needs:

- Distant branch cracks from outer lanes.
- Directional low-frequency calls.
- Wind shifts from the pursuer's region.
- Audio cues that imply distance without revealing exact location.
- Silence or muffling near dangerous tiles.
- Unique ambience per tile type or biome.

The player should not need debug information to feel the board's pressure.

### What Needs to Become Environmental or Diegetic

The hidden grid should express itself through:

- Trail junctions.
- Sightlines.
- Repeated landmarks.
- Natural boundaries.
- Tree density.
- Terrain transitions.
- Environmental storytelling.
- Subtle navigational cues.

Board-game information should become diegetic information.

Examples:

- Row/column marker becomes notches on trees, map fragments, compass cues, or landmarks.
- Tile type becomes terrain identity.
- Tile orientation becomes trail direction or landmark facing.
- Pursuer position becomes sound, tracks, broken branches, or environmental changes.

### What Data/Logic Should Still Exist Under the Hood

Phase 2 should preserve:

- `FrameRows = 26`.
- `FrameColumns = 26`.
- 676 total Slots.
- Tile bank size of 1000 unless intentionally redesigned.
- Tile ID assignment.
- Spawn Tile IDs `000` and `666`, at least as prototype constants.
- Orientation index `0-5`.
- Field generation report.
- Slot address lookup.
- Runtime Slot inspection.
- Region-based spawn rules.
- Ability to rebuild the same Field structure from data if needed.

### Unity Systems or Scene Objects Needed

Phase 2 may need:

- `WorldGenerationManager`.
- `FrameData` or `FieldData` ScriptableObject/runtime model.
- `SlotData` model.
- `TileDefinition` ScriptableObjects.
- `TilePrefabRegistry`.
- `TileChunkSpawner`.
- `ChunkConnector` or path-link system.
- `PlayerSpawnManager`.
- `PursuerSpawnManager`.
- `PursuerController`.
- `FieldDebugReporter`.
- `SlotDebugOverlay`.
- `RuntimeDebugRaycaster`.
- `WorldStreamingManager` if the full 676-cell world is too large.
- NavMesh generation or pre-authored navigation per chunk.
- Audio zone manager.
- Lighting/fog manager.
- Save/load support for generated Field state.

## 9. Risks for Phase 2

### Scale Risk

A `26 x 26` grid can become physically huge in first-person. 676 3D chunks may be too large to render or navigate comfortably if each Slot becomes a full scene-scale area.

Mitigation:

- Use smaller chunks.
- Stream chunks.
- Generate only nearby chunks.
- Use fog, terrain occlusion, and culling.
- Consider grouping multiple logical Slots into larger spatial regions if needed.

### Literal Hex Risk

If Phase 2 preserves visible hexes too literally, the first-person world may feel artificial or board-like.

Mitigation:

- Keep hex logic hidden.
- Blend terrain boundaries.
- Let paths and natural barriers define movement.
- Avoid visible hex outlines.

### Debug UI Carryover Risk

The Phase 1 labels and UI are useful but not atmospheric.

Mitigation:

- Keep debug overlays editor-only or behind a dev toggle.
- Do not expose Tile IDs or Slot addresses to the player unless intentionally diegetic.

### Randomness Without Meaning

Random Tile placement may feel arbitrary in 3D if Tiles do not have rules for adjacency, biome consistency, path continuity, or encounter pacing.

Mitigation:

- Add tile types.
- Add connection rules.
- Add biome regions.
- Add constraints.
- Add validation passes after generation.

### Orientation Complexity

Orientation will matter more in 3D, especially if chunks have paths or entrances.

Bad orientation handling could create disconnected paths or impossible terrain.

Mitigation:

- Define connection sockets per Tile.
- Rotate sockets with orientation.
- Validate neighbor compatibility.
- Use debug visualizers for exits and links.

### Pursuer Navigation Risk

The pursuer starting in the outer lanes is simple in 2D, but in 3D the pursuer needs believable navigation.

Mitigation:

- Keep pursuer logic aware of Slot graph.
- Use NavMesh or graph movement.
- Let the pursuer move through logical Slots even if visual terrain is complex.
- Separate high-level Slot pursuit from low-level 3D locomotion.

### Player Navigation Risk

A first-person forest can become confusing in a bad way.

Lost Forest should feel disorienting, not unreadable.

Mitigation:

- Use landmarks.
- Use sound direction.
- Use subtle environmental gradients.
- Give players partial navigation tools.
- Avoid making every Slot visually identical.

### Performance Risk

Runtime generation of hundreds of meshes, colliders, labels, audio zones, and objects may become expensive.

Mitigation:

- Use prefab pooling.
- Stream or chunk.
- Reduce collider complexity.
- Avoid runtime text labels in production mode.
- Use baked or simplified terrain pieces.

## 10. Do Not Carry Forward

The following should not be preserved literally in Phase 2:

- Visible hex grid as player-facing world presentation.
- Slot address labels on the ground.
- Tile ID labels on the ground.
- Orientation labels on the ground.
- Green and red spawn colors as in-world visual markers.
- The temporary Start button as final UI.
- The selected Slot debug window as player UI.
- Full board camera framing.
- The idea that the player sees the whole Field at once.
- Debug-only row and column markers as literal objects.
- Tile IDs as player-facing names.
- Purely random Tile placement without later design constraints.
- All non-spawn Tiles being identical.
- Placeholder win/loss logic.
- Bootstrap-only scene setup as final architecture.
- Click-to-select as player interaction, unless redesigned as an inspection/debug tool.
- The exact 2D mesh generation approach as the final 3D environment approach.
- Unity TextMesh labels as runtime production elements.
- Treating the board as complete gameplay rather than underlying world logic.

## 11. Open Questions

Phase 2 needs to answer:

- What does one Slot physically represent in first-person scale?
- Is one Slot one terrain chunk, one room-like forest clearing, or one logical zone?
- Should all 676 Slots exist physically at once?
- Should the world stream around the player?
- How large should a Slot be in meters?
- How should neighboring Slots connect?
- Do Tiles have path sockets?
- What tile types exist?
- Are tile types drawn from the same `000-999` bank or defined separately?
- Does Tile ID determine content, or is Tile ID only an instance reference?
- Should Tile `000` and Tile `666` remain special forever or only for prototype/debug?
- What does the player actually see at spawn?
- How does the pursuer enter the player's awareness?
- Does the player know there is a pursuer immediately?
- Does the pursuer move on the Slot graph, in physical space, or both?
- What is the first win condition?
- What is the first loss condition?
- How does the player navigate without seeing the grid?
- Should a map exist?
- Should coordinates ever become diegetic?
- How much randomness should be constrained by biome or path logic?
- Can the Field be saved and reloaded?
- Should generation be seed-based for reproducible debugging?
- What is the minimum 3D slice needed to prove the Phase 2 translation?

## 12. Recommended First Phase 2 Tasks

1. Create a new low-poly first-person Unity test scene.
2. Preserve the Phase 1 data model separately from visuals: Frame, Field, Slot, Tile, Orientation, and Spawn rules.
3. Create a simple `TileDefinition` or prefab registry with Tile ID, 3D prefab, tile type, connection points, debug color, and spawn role.
4. Build a small 3D proof-of-concept Field, starting with `5 x 5` or `7 x 7` instead of full `26 x 26`.
5. Keep the same Slot/Tile concepts.
6. Spawn the player near the center.
7. Spawn the pursuer or placeholder threat near the edge.
8. Create 3-5 low-poly tile chunk prefabs: dense forest, clearing, trail straight, trail bend, and landmark or ruin.
9. Implement tile orientation in 3D by rotating chunk prefabs based on orientation index.
10. Confirm path openings rotate correctly.
11. Add a first-person controller with simple walking, mouse look, and collision.
12. Add a debug overlay showing current Slot address, current Tile ID, tile type, orientation, and pursuer Slot if active.
13. Add a Field report for the 3D version, including Slot, Tile, orientation, prefab, type, and role.
14. Prototype pursuer presence with a marker, sound source, or abstract moving debug object.
15. Keep high-level movement on the Slot graph first and translate to 3D behavior later.
16. Test scale and determine how large one Slot should feel.
17. Check whether moving between Slots feels like walking through a forest rather than crossing board cells.
18. Decide what the player should understand: whether they know they are in a generated forest, whether they know the pursuer exists, and whether they have tools, map fragments, compass behavior, or only environmental cues.

The most important Phase 2 goal is not to make the whole `26 x 26` world immediately. The first goal is to prove that the hidden Phase 1 board logic can produce a small first-person forest space that feels spatial, atmospheric, readable, and tense.
