# Lost Forest Phase 2 - Board and Tile Construction Spec

## Purpose

This spec defines how Lost Forest Phase 2 should use a hidden board/tile structure to construct a first-person 3D forest.

Phase 2 does not show a board to the player. The board exists as a construction, rules, debug, and tuning layer underneath the 3D world. Each hidden 2D tile has a tile ID. Each tile ID maps to a 3D hex-shaped landscape chunk, chunk recipe, or prefab variant. The player experiences a continuous snowy forest, while the game uses the hidden tile layout for placement, adjacency, rune rules, terrain meaning, home placement, pressure, and streaming.

## Phase 1 Translation

Phase 1 proved that Lost Forest works when the forest is secretly structured as tiles with randomized layout, route pressure, rune search, home return, movement cost, and pursuer escalation.

Phase 2 should keep the logic and discard the presentation.

Primary Phase 1 source:

- [Phase1_Hex_And_Board_Construction_Handoff](../Handoffs/Phase1_Hex_And_Board_Construction_Handoff.md)

Carry forward:

- Tile identity as game logic.
- Random tile draw and layout.
- Adjacency-driven movement and distance relationships.
- Central home / standing-stone anchor.
- Rune-eligible spaces.
- Terrain and elevation as route pressure.
- Landmarks as orientation information.
- Pursuer pressure tied to distance, danger, noise, and rune progress.
- Debug overlays that reveal hidden structure to developers.

Discard:

- Visible player-facing board.
- Tile labels, row/column labels, and visible IDs.
- Click-to-move board interaction.
- Top-down board camera.
- Board-game tokens or tabletop presentation.

Internal rule:

The hidden tile layer may behave like a board. The player-facing forest must not look like one.

## Inherited Vocabulary

Phase 2 should preserve the useful Phase 1 terms because they describe different responsibilities clearly:

- Frame: the empty addressable world structure before content is placed.
- Slot: one addressable cell in the Frame.
- Tile: the content assigned to a Slot.
- Field: the completed generated world state after Tiles are assigned to Slots.

Phase 1 used a `26 x 26` Frame:

- 676 total Slots.
- Rows `A-Z`.
- Columns `1-26`.
- Slot addresses such as `A1`, `M14`, and `Z26`.
- Tile bank IDs `000-999`.
- 676 unique Tiles drawn per full Field.
- 324 Tiles unused after a full Field is generated.
- Six orientation indices, `0-5`, mapping to `0`, `60`, `120`, `180`, `240`, and `300` degrees.

Phase 2 should keep this as the canonical full-world data model unless intentionally redesigned. The first Unity slice may use fewer Slots, but it should still use the same concepts.

## Core Model

Phase 2 world construction has three layers:

1. Tile layout layer.
   A hidden Frame/Field layout containing Slots, tile IDs, adjacency, orientation, terrain meaning, rune eligibility, and runtime state.

2. Chunk presentation layer.
   A 3D world layer that maps tile IDs to hex-shaped terrain chunks and places them at matching world positions.

3. Gameplay interpretation layer.
   Systems that read tile data to drive stamina cost, chill, rune placement, landmarks, home logic, pursuer pressure, and debug information.

The first implementation should prove these layers are separate. Tile rules should work even if the 3D chunk art is replaced.

## Coordinate System

Use two coordinate views:

1. Designer/debug address.
   The Phase 1-style Slot address, such as `M14`, remains the human-readable reference for reports, debug tools, bug reports, teleport commands, and designer notes.

2. Hex math coordinate.
   Axial coordinates drive neighbor lookup, distance, chunk placement, and graph movement.

Use axial hex coordinates for runtime hex math:

- `q`: diagonal column axis.
- `r`: row axis.
- Derived `s`: `-q - r`, used for distance and validation.

Hex neighbors:

- East: `(+1, 0)`
- Northeast: `(+1, -1)`
- Northwest: `(0, -1)`
- West: `(-1, 0)`
- Southwest: `(-1, +1)`
- Southeast: `(0, +1)`

Store adjacency explicitly at runtime after layout generation, even if it can be derived from coordinates. This makes later constraints, blocked borders, cliffs, streams, gates, and special transitions easier to support.

For the full `26 x 26` Frame, row/column addresses should map deterministically to axial coordinates. The exact conversion can be implementation-specific, but it must be stable, reportable, and reversible for debugging.

## Tile Definition Data

`TileDefinition` is static authored data. It describes what a tile ID means before it is placed in a run.

Suggested fields:

- `tileId`: stable numeric ID.
- `debugName`: human-readable development name.
- `isUniqueTile`: whether this ID is used once per Field, matching Phase 1 behavior.
- `chunkPrefab`: default 3D hex chunk prefab.
- `chunkVariants`: optional visual variants for the same gameplay tile.
- `terrainType`: clearing, dense forest, ridge, hollow, creek, dead grove, boulder field, slope, home, or other prototype tags.
- `elevationBand`: low, mid, high, or peak.
- `edgeProfile`: six edge descriptors used for stitching and adjacency validation.
- `landmarkSlots`: authored anchor points for landmarks inside the chunk.
- `requiredLandmarkTags`: landmarks this tile should always contain.
- `optionalLandmarkTags`: landmarks this tile may contain.
- `runeEligible`: whether a rune may spawn here.
- `homeEligible`: whether standing stones may be placed here.
- `minDistanceFromHomeForRune`: integer hex distance.
- `dangerValue`: base pressure value for pursuer and tuning.
- `visibilityValue`: rough sightline/readability value.
- `chillModifier`: local change to chill gain.
- `staminaModifier`: local change to traversal cost.
- `noiseModifier`: local change to how much player movement carries.
- `spawnWeight`: random draw weight.
- `reservedRole`: none, player spawn, pursuer spawn, home, required landmark, or other special placement role.
- `tags`: flexible labels for rules and debug filtering.

Tile IDs should be stable across builds. If a tile meaning changes significantly, create a new tile ID instead of silently reusing the old one.

Phase 1 reserved Tile `000` for the player spawn and Tile `666` for the pursuer spawn. Phase 2 should preserve those IDs as prototype constants unless a later design pass intentionally separates spawn role from tile identity.

## Tile Instance Data

`TileInstance` is runtime data. It describes one placed tile in one generated layout.

Suggested fields:

- `instanceId`: unique runtime identifier.
- `tileId`: reference to `TileDefinition`.
- `slotAddress`: human-readable address such as `M14`.
- `rowIndex`: zero-based Frame row.
- `columnIndex`: zero-based Frame column.
- `coord`: axial coordinate.
- `worldPosition`: 3D chunk origin.
- `rotationSteps`: 0-5 hex rotations.
- `neighbors`: six neighbor instance references or coordinates.
- `edgeStates`: six runtime edge states such as open, blocked, slope, creek, cliff, seam, or debug-only.
- `regionId`: optional region grouping.
- `distanceFromHome`: computed hex distance.
- `isHomeTile`: true for the standing-stone tile.
- `role`: field, player spawn, pursuer spawn, home, rune, landmark, or other runtime role.
- `isRuneEligibleThisRun`: runtime rune eligibility after layout rules.
- `hasRune`: whether a rune spawned here.
- `hasLandmark`: whether a major landmark occupies this tile.
- `chunkInstance`: spawned 3D object reference.
- `runtimeFlags`: discovered, visited, streamed, disabled, reserved, or debug flags.

Do not put player-facing UI data in `TileInstance`. It is a hidden world state object.

## Tile ID to 3D Chunk Mapping

Each tile ID maps to one gameplay meaning and one or more 3D chunk presentations.

Prototype mapping:

- Tile ID `000`: player spawn / home clearing / standing-stone center for the first pass.
- Tile ID `666`: pursuer spawn / outer-lane threat origin for prototype pressure.
- Tile ID `001`: optional non-spawn home clearing if spawn and home are later separated.
- Tile IDs `010-019`: basic forest variants.
- Tile IDs `020-029`: dense forest / low visibility.
- Tile IDs `030-039`: ridges / high elevation.
- Tile IDs `040-049`: hollows / low elevation.
- Tile IDs `050-059`: frozen creek or wet lowland.
- Tile IDs `060-069`: boulder fields and rock landmarks.
- Tile IDs `070-079`: dead grove / high danger.
- Tile IDs `080-089`: clearings / high readability.
- Tile IDs `090-099`: special authored landmark tiles.

The numeric ranges are only an organization aid. Gameplay code should use data fields and tags, not numeric range checks.

Mapping rules:

- One tile ID may have several visual chunk variants if gameplay meaning is identical.
- One 3D chunk prefab should not represent multiple unrelated gameplay meanings.
- Rotation is allowed, but the tile definition must declare whether its edge profile remains valid under rotation.
- Landmark and rune spawn sockets should be part of the chunk prefab or recipe.
- Edge profiles must be compatible with neighboring chunks or resolved by seam filler.

## Adjacency Rules

Adjacency exists in the hidden layout and should be validated before chunks spawn.

Basic first-pass adjacency:

- Any tile can neighbor any other tile if the coordinate exists.
- Movement between adjacent tiles is allowed unless an edge is blocked.
- Hex distance uses axial/cube distance.
- Debug tools can draw all adjacency links.

Prototype validation rules:

- Home tile must have at least four open neighbors.
- Rune tiles must be reachable from home.
- No required objective tile may be isolated by blocked edges.
- High cliffs should not directly border low hollows unless a transition edge or slope tile exists.
- Creek edges should continue into creek-compatible edges when possible.
- Major landmarks should not cluster unless intentionally authored.

Phase 2 first pass should prefer permissive adjacency. Add stricter constraints only after the fixed 19-tile layout works.

## Random Tile Draw and Layout

World generation should start with a deterministic seed.

First-pass sequence:

1. Create a small proof Frame, initially radius 2 for 19 total tiles or a `5 x 5` / `7 x 7` rectangular subset.
2. Assign stable Slot addresses and axial coordinates.
3. Reserve Tile `000` for player/home placement.
4. Reserve Tile `666` for pursuer/threat-origin placement if the prototype includes the pursuer pressure layer.
5. Place Tile `000` in the middle region, initially the center Slot.
6. Place Tile `666` in an outer lane or edge Slot.
7. Reserve required structural tiles if needed, such as one ridge, one hollow, one clearing, and one dense forest tile.
8. Fill remaining Slots by weighted random draw from the tile pool.
9. Apply orientation index `0-5` to tiles that support rotation.
10. Build adjacency references.
11. Validate reachability from home.
12. Select rune-eligible tiles.
13. Place landmarks.
14. Spawn or stream matching 3D chunks.

Random draw constraints:

- Do not place runes on the home tile.
- Do not place runes adjacent to home in the default first pass.
- Ensure at least one rune-eligible tile exists at distance 2 or greater.
- Avoid placing too many high-danger or low-visibility tiles adjacent to home.
- Guarantee at least one readable route out of home using clearings, gentle slopes, or landmarks.
- Keep unique Tile IDs within one generated Field unless a later design explicitly allows repeat content IDs with separate instance IDs.
- Keep the full `26 x 26` / 1000 Tile bank model available in data, but do not require the first 3D slice to instantiate all 676 Slots.

If validation fails, regenerate from the same seed with an incremented attempt counter. After a small maximum number of attempts, fall back to a known-good layout.

## Home and Standing-Stone Placement

Home is the emotional and mechanical anchor of the run.

First-pass rules:

- Home tile is placed in the middle region.
- In the smallest proof layout, home is at axial coordinate `(0, 0)`.
- In the full Phase 1 Frame, home/player spawn uses middle-region logic, historically rows `J-Q` and columns `10-17`.
- Home/player spawn uses Tile `000` for the prototype unless spawn and home are intentionally separated.
- Standing stones spawn from a dedicated socket or center anchor on the home chunk.
- Home tile must be readable from nearby chunks by silhouette, clearing shape, sound, or light treatment.
- Home should connect to multiple route options, not a single corridor.
- Home is never rune-eligible.
- Home should have lower base danger than outer tiles.

Standing-stone responsibilities:

- Start point.
- Rune deposit location.
- Progress marker.
- Temporary safety reference.
- Debug origin for layout and distance calculations.

Later versions may support off-center home placement or larger layouts, but the first pass should keep home central.

## Pursuer Spawn Placement

Phase 1 reserved Tile `666` for the pursuer spawn and placed it in the outer three lanes.

Phase 2 should preserve the spatial relationship:

- Player/home begins near the middle.
- Pursuer/threat begins near the edge.
- The Field exists between them.

First-pass rules:

- Use Tile `666` as the prototype pursuer/threat-origin tile.
- Place it on an edge or outer-lane Slot of the small proof layout.
- In a full `26 x 26` Frame, outer lanes mean rows `A-C`, rows `X-Z`, columns `1-3`, or columns `24-26`.
- Do not expose Tile `666` visually to the player.
- Debug tools may show it as a special role.
- Early pursuer implementation may be a marker, sound source, pressure origin, or graph agent rather than a final 3D character.

The high-level pursuer should understand the Slot graph even if low-level 3D movement later uses NavMesh or another locomotion system.

## Rune Eligibility

Rune eligibility is data-driven and then filtered by runtime layout rules.

Static requirements:

- Tile definition has `runeEligible = true`.
- Tile has at least one valid rune socket.
- Tile terrain supports discovery, such as clearing edge, stone, dead tree, creek bend, hollow, or boulder cluster.

Runtime requirements:

- Tile is not home.
- Tile is reachable from home.
- Tile is at or beyond the configured minimum home distance.
- Tile is not occupied by a conflicting required landmark.
- Tile is not immediately adjacent to another selected rune tile unless clustering is intentional.
- Tile does not exceed first-pass danger limits unless testing high-risk rune placement.

Recommended first pass:

- Spawn one rune per run.
- Select from eligible tiles at distance 2.
- Prefer tiles with strong local landmark context.
- Show rune location only in debug mode.

Later:

- Multiple runes.
- Rune tiers.
- Rune clue chains.
- Escalating distance requirements after deposits.

## Terrain, Elevation, and Landmark Meaning

Terrain tags should affect both navigation feel and system tuning.

Suggested meanings:

- Clearing: high visibility, easier orientation, higher exposure.
- Dense forest: low visibility, stronger disorientation, higher pursuer ambiguity.
- Ridge: high elevation, better sightlines, higher stamina cost, stronger wind/chill.
- Hollow: low elevation, reduced sightlines, possible chill pocket, easier concealment.
- Creek: directional navigation cue, possible movement channel, sound masking.
- Boulder field: strong landmark silhouettes, uneven movement, possible rune context.
- Dead grove: danger signal, low comfort, good rune/pressure candidate.
- Slope: traversal cost and visual direction cue.
- Home clearing: safety anchor and return target.

Elevation should be stored as tile-level meaning first, not final terrain mesh precision. The first pass only needs enough elevation data to alter stamina, sightline intent, and chunk edge compatibility.

Landmarks should attach through data:

- Major landmark: unique or nearly unique navigation anchor, usually one tile.
- Medium landmark: repeated but recognizable cue.
- Minor landmark: local dressing used for route memory.

Each tile can expose landmark sockets. The layout system chooses which sockets to fill based on tile definition, spacing rules, and run seed.

## Chunk Stitching and Streaming

First-pass chunk construction should be simple and reliable.

Stitching rules:

- All chunks share the same hex footprint.
- Neighbor chunk origins are calculated from axial coordinates.
- Edge heights should match by profile where possible.
- Use seam filler strips, snow banks, rocks, roots, shrubs, fog, or tree clusters to hide imperfect joins.
- Prototype terrain should avoid extreme edge height differences until transition chunks exist.

Streaming rules:

- Initial prototype may spawn all 19 chunks.
- Streaming begins after fixed placement is proven.
- Keep current tile, neighbor ring, and one outer ring active.
- Deactivate or simplify distant chunks.
- Never unload home, active rune, or active pursuer-relevant chunks during first-pass testing.
- Streaming state must not change tile identity or gameplay rules.

The tile layout is authoritative. Chunk streaming is presentation and performance behavior.

## Debug Tools

Debugging is part of the Phase 2 design, not optional polish.

Required first-pass debug tools:

- Toggle hidden tile overlay.
- Show tile IDs.
- Show Slot addresses.
- Show axial coordinates.
- Show current player tile.
- Show chunk boundaries.
- Show adjacency links.
- Show home tile and home distance.
- Show rune-eligible tiles.
- Show selected rune tile.
- Show tile terrain/elevation/danger values.
- Show spawned landmark labels.
- Show world seed and generation attempt.
- Export or display a Field Slot Report.
- Regenerate layout with seed.
- Teleport to tile coordinate.
- Teleport to Slot address.
- Force rune spawn on selected tile.
- Toggle chunk streaming radius.

Useful soon after:

- Heatmap for chill, danger, visibility, or stamina cost.
- Route trace showing player path.
- Edge compatibility warnings.
- Reachability validation report.
- Tile pool distribution summary.
- Screenshot/export of generated 2D layout.

Debug UI must remain development-only and visually distinct from player-facing feedback.

## First-Pass Scope

Build:

- 19-tile hidden axial layout.
- Phase 1 vocabulary in data: Frame, Slot, Tile, Field.
- Stable Slot addresses even in the small proof layout.
- Stable tile ID definitions.
- Tile `000` as player/home spawn for the prototype.
- Tile `666` as pursuer/threat-origin spawn for the prototype.
- Home tile near center.
- Pursuer/threat tile near edge.
- Weighted random draw after fixed layout works.
- 3D placeholder hex chunks.
- Tile ID to chunk prefab mapping.
- Basic adjacency and reachability validation.
- One rune spawn on a valid eligible tile.
- Standing stones on home tile.
- Terrain/elevation tags with simple gameplay modifiers.
- Landmark sockets and simple landmark placement.
- Debug overlay for tile/chunk/rune/home data.
- Field Slot Report equivalent for the 3D version.

Do not build yet:

- Infinite procedural generation.
- Full physical `26 x 26` world with all 676 chunks instantiated.
- Final forest art.
- Final terrain blending.
- Complex biome system.
- Save/load.
- Full inventory.
- Multi-rune progression beyond a simple loop.
- Complex AI navigation.
- Final pursuer model.
- Final audio implementation.
- Player-facing map, compass, or board UI.
- Heavy optimization before the 19-tile loop works.

## Acceptance Criteria

The first implementation of this spec is successful when:

- A seed creates a hidden 19-tile hex layout.
- Each placed tile has a stable tile ID and axial coordinate.
- Each placed tile has a stable Slot address.
- Each tile spawns the matching 3D hex chunk.
- Chunk positions align into a walkable forest space.
- The player can stand in the world without seeing board-game presentation.
- Debug mode can reveal tile IDs, coordinates, boundaries, and adjacency.
- Home standing stones spawn at the center tile.
- Tile `000` is reserved for the prototype player/home spawn.
- Tile `666` is reserved for a prototype pursuer/threat origin if the pursuer layer is enabled.
- At least one rune spawns on a valid non-home eligible tile.
- The player can pick up or activate the rune and return it to home.
- Terrain/elevation data can affect at least one prototype value such as stamina, chill, or movement cost.
- The system can regenerate the same layout from the same seed.

## Implementation Notes

Keep the first code path boring and data-driven:

- Use `ScriptableObject` assets for `TileDefinition`.
- Use plain runtime classes or structs for `TileInstance`.
- Use a `Frame` / `Field` runtime model to preserve the Phase 1 structure without coupling it to visuals.
- Keep generation in `TileLayoutManager`.
- Keep prefab spawning in `ChunkStreamingManager`.
- Keep rune selection in `RuneManager`, but make it query tile layout data.
- Keep standing-stone placement tied to home tile data.
- Keep debug drawing separate from gameplay rules.
- Keep the Field Slot Report extensible, with columns for Slot address, tile ID, orientation, role, chunk prefab, terrain type, rune state, landmark state, and pursuer/debug state.

Initial Phase 2 owned source starts in:

- `Assets/LostForest/Scripts/World`

This source should borrow Phase 1 concepts, constants, and proven generation rules, but it should not inherit Phase 1's visible-board mesh, label, camera, or click-selection assumptions.

The tile system should be the quiet skeleton of the forest: invisible to the player, obvious to the developer, and stable enough for every later system to trust.
