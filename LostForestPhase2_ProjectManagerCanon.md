# Lost Forest Phase 2 - Project Manager Canon

## Phase 2 Mission

Phase 2 is a new low-poly first-person Unity prototype meant to prove that the core Lost Forest experience works in 3D.

The prototype should test whether the player can feel lost, oriented, pressured, and curious inside a snowy birch/aspen forest without relying on a board-game interface. The goal is not visual polish, cinematic horror, or a complete content set. The goal is to translate the proven Phase 1 loop into embodied first-person play:

- Enter a confusing forest.
- Read terrain, landmarks, sound, and environmental cues.
- Find hidden runes.
- Return runes to a central standing-stone site.
- Manage stamina, chill, sprint, and route choice.
- Feel pursued by something mostly unseen.

Phase 2 should answer one core question:

Can Lost Forest become a tense, navigable, replayable first-person forest game while keeping the strategic pressure and route-reading that made Phase 1 work?

Phase 2 should be built as the proven functioning structure that Phase 3 can wrap in final-quality art, sound, animation, and atmosphere. The prototype can look simple, but the underlying systems should be real enough to survive into later production.

This means Phase 2 should prioritize:

- Clear system boundaries.
- Replaceable placeholder visuals.
- Data-driven tile, region, rune, and pursuer logic.
- Debug tools that make tuning fast.
- A playable loop that works before it looks beautiful.
- Architecture that allows Phase 3 to improve presentation without rebuilding the core game.

## Relationship to Phase 1

Phase 1 proved the core game loop in a 2D board-game form. It established that a hidden forest structure, randomized layout, rune search objective, home-return loop, stamina/chill pressure, movement cost, sprint decisions, and a pursuing entity can create a playable survival-navigation rhythm.

Phase 2 should inherit the logic of Phase 1, not its literal presentation.

Carry forward:

- Hidden cell and region structure.
- Tile identity as world logic.
- Randomized tile draw / layout logic.
- Rune search and return loop.
- Central standing stones / home point.
- Pursuer pressure.
- Stamina, chill, sprint, and route choice.
- Terrain as navigation language.
- Landmarks as orientation tools.
- Debug tools for development.

Do not carry forward literally:

- Visible hex board.
- Click-to-move controls.
- Tile ID labels.
- Row/column labels.
- Debug-heavy UI as player-facing presentation.
- Cylinder tokens.
- Top-down board camera.
- Board-game visual language.

Phase 1 is now an archived reference project. Phase 2 should be treated as a new prototype with its own architecture, scene structure, controls, and presentation.

## Phase 2 Design Pillars

1. First-person disorientation, not random confusion.
   The forest should make the player uncertain, but not helpless. The player should learn to read patterns, slopes, clearings, tree density, sound, and landmark silhouettes.

2. Navigation without a compass.
   Orientation should come from environmental literacy. The player should use standing stones, ridgelines, unusual trees, clearings, water, rock forms, wind, distant sound, and memory.

3. Terrain is information.
   Slopes, elevation, snow cover, forest density, sightlines, and traversal cost should shape decisions. Terrain should communicate risk, opportunity, and direction.

4. The rune loop stays simple and legible.
   Find runes in the forest. Carry or mark them. Return them to standing stones. Each successful return should deepen the run and increase pressure.

5. Pressure is systemic, not scripted.
   Stamina, chill, limited visibility, route choice, and pursuer proximity should combine into tension without relying on constant jump scares.

6. The pursuer is mostly unseen.
   The entity should be felt through audio, glimpses, tracks, disturbed snow, broken sightlines, and behavioral pressure. It should not become a fully readable combat enemy early in Phase 2.

7. Low-poly clarity over realism.
   Visuals should be simple, readable, and atmospheric. The prototype needs strong silhouettes, clean landmarks, and navigable terrain before high-fidelity assets.

8. Debuggability is part of the design process.
   Phase 2 needs strong development tools: region overlays, rune locations, pursuer state, player stats, route traces, and spawn controls. These should remain separate from player-facing presentation.

## Core Player Experience

Moment to moment, the player should move through a snowy first-person forest while trying to keep a mental map of where they are.

They should notice a distinctive rock, a leaning birch cluster, a slope down into a hollow, a distant standing-stone silhouette, or the sound of wind through a clearing. They should make decisions based on partial information:

- Do I climb the ridge for visibility, even if it costs stamina?
- Do I sprint across an exposed clearing before chill worsens?
- Do I follow the low ground where movement is easier, even if I lose sight of home?
- Do I turn back now with one rune, or push farther for another?
- Was that sound behind me, or just the forest?

The player should feel small in the forest, but not powerless. Skill should come from learning the world language: landmarks, slope, visibility, sound, and risk.

The standing stones should function as home, ritual center, and emotional anchor. Leaving them should feel like entering uncertainty. Returning should feel like relief, progress, and temporary safety.

## Systems To Translate

### World/region structure

Phase 2 should preserve the idea of hidden cells or regions, but express them as invisible world logic rather than a visible hex grid.

Important bridge from Phase 1 to Phase 2:

Phase 2 should still be built from tiles. The difference is that the player no longer sees a flat board. Instead, the game uses the board/tile layer as hidden construction logic, then presents the result as a continuous first-person 3D forest.

Each tile should have an ID number and a corresponding 3D landscape version. World construction can begin with a 2D tile layout or draw system, identify the selected tile IDs, then stream or stitch the matching 3D hex-shaped landscape chunks into the world as the player moves.

In other words:

- Phase 1 tile = visible board space.
- Phase 2 tile = hidden construction unit with a 3D hex landscape chunk.
- Phase 1 board layout = reference logic for random tile draw and adjacency.
- Phase 2 world = stitched 3D chunks generated from that hidden tile layout.

The player should experience a continuous snowy forest, but the underlying world can still be composed of hex-shaped 3D chunks with tile IDs, adjacency rules, terrain tags, landmarks, rune eligibility, and debug visibility.

Possible translation:

- World divided into hidden hex-shaped 3D landscape tiles, regions, zones, or cells.
- Each tile has a stable ID and can map to a specific 3D chunk prefab, scene section, or generated terrain recipe.
- Each tile has terrain tags, landmark rules, rune spawn eligibility, navigation cues, adjacency rules, and danger values.
- A 2D layout layer can draw/select tile IDs before the corresponding 3D chunks are loaded or stitched.
- Nearby chunks can stream in around the player while distant chunks remain unloaded, simplified, or abstract.
- Region boundaries are not displayed to the player.
- Debug mode can show tile IDs, region IDs, chunk boundaries, adjacency links, heat values, and spawn data.

### Terrain/elevation

Phase 1 movement cost from elevation should become embodied traversal pressure.

Possible translation:

- Slopes affect stamina drain, movement speed, chill exposure, or sound.
- High ground provides visibility but costs stamina and may expose the player.
- Low ground may be easier to traverse but harder to navigate from.
- Terrain regions can support route-choice tradeoffs.

### Landmarks/navigation

Landmarks should replace board labels and visible coordinates.

Possible translation:

- Large landmarks: standing stones, ridge, frozen creek, fallen tree, boulder cluster, dead grove, watch tree, hollow, clearing.
- Medium landmarks: unusual tree shapes, cairns, animal tracks, broken branches, exposed roots.
- Directional cues: wind, distant tones, light falloff, slope direction, tree density, snow texture, sound occlusion.

### Runes/standing stones

Runes should remain the core objective.

Possible translation:

- Runes spawn in eligible hidden regions.
- Runes may emit subtle audio/visual cues at close range.
- Player collects or activates a rune in the forest.
- Player returns to central standing stones to deposit it.
- Depositing runes updates progression, pressure, world state, or pursuer behavior.

### Player movement

Phase 2 should use simple first-person movement with enough physicality to make terrain matter.

Possible translation:

- Walk, sprint, and possibly crouch or careful movement later.
- Grounded first-person controller.
- Stamina-aware sprinting.
- Slope-aware movement costs.
- Footstep audio variation by surface and state.

### Stamina/chill/sprint

The pressure economy should remain central.

Possible translation:

- Stamina drains from sprinting, climbing, panic movement, or difficult terrain.
- Stamina recovers while walking or resting.
- Chill rises over time and may rise faster in exposed areas, wind, deep snow, or while exhausted.
- Chill may reduce stamina recovery, visibility, hearing, or control responsiveness.
- Sprint is useful but costly, especially when pursued.

### Pursuer logic

The pursuer should create pressure without becoming the whole game.

Possible translation:

- Pursuer has awareness, interest, search, stalk, and close-pressure states.
- Pursuer reacts to player noise, sprinting, region danger, rune progress, and time.
- Most feedback is indirect: sound, glimpses, disturbed trees, tracks, breath, shadow, camera/audio pressure.
- Debug tools should expose exact state, distance, target region, and behavior.

### UI/feedback

Player-facing UI should be minimal and atmospheric.

Possible translation:

- No grid labels, no tile IDs, no board UI.
- Stamina and chill can be represented through subtle HUD, breath, vignette, sound, hand animation, or movement response.
- Rune progress should be clear at standing stones.
- Debug UI should be available only in development mode.

### Debug tools

Phase 2 should include debug tools from the start.

Possible translation:

- Toggle region overlay.
- Show rune spawn locations.
- Show standing-stone/home marker.
- Show pursuer state and path target.
- Show stamina/chill values.
- Teleport to regions or landmarks.
- Force rune spawn.
- Force pursuer state.
- Draw player route trail.

## Early Technical Architecture

Phase 2 should start with a clean Unity architecture built for iteration. Avoid heavy content systems until the first loop is playable.

Likely scene-level managers:

- `GameManager`
  Owns run state, high-level game phase, win/loss conditions, and restart flow.

- `WorldManager`
  Builds or initializes the forest test world, registers regions, landmarks, rune spawn points, and standing stones.

- `TileLayoutManager`
  Draws, stores, or generates the hidden 2D tile layout and resolves tile IDs, adjacency, region membership, and tile metadata.

- `ChunkStreamingManager`
  Loads, unloads, pools, or activates the 3D landscape chunks that correspond to nearby tile IDs as the player moves.

- `RegionManager`
  Tracks hidden region/cell data, player current region, region tags, danger values, terrain modifiers, and debug overlays.

- `RuneManager`
  Handles rune spawning, discovery, collection/activation, carried rune state, deposited rune count, and standing-stone interactions.

- `StandingStoneManager`
  Owns home point behavior, deposit logic, ritual progress, safe radius rules, and home feedback.

- `PlayerStatsManager`
  Owns stamina, chill, sprint state, recovery, drain modifiers, and exposed events for UI/audio/feedback.

- `PursuerManager`
  Owns pursuer state machine, pressure pacing, awareness, target selection, and debug reporting.

- `AudioCueManager`
  Handles environmental cues, rune cues, pursuer cues, cold/stamina feedback, and directional audio hooks.

- `DebugManager`
  Centralizes debug toggles, overlays, development commands, and visible diagnostic UI.

Likely player components:

- `FirstPersonController`
  Movement, camera look, ground checks, slope handling, sprint input, and movement events.

- `PlayerInteractor`
  Handles looking at and interacting with runes, standing stones, and debug test objects.

- `PlayerRegionTracker`
  Reports current hidden region to the region/world systems.

- `PlayerFeedbackController`
  Converts stamina, chill, rune, and pursuer events into camera, audio, animation, and HUD feedback.

Likely data objects:

- `TileDefinition`
  Tile ID, chunk prefab reference, terrain tags, edge connection data, landmark slots, rune eligibility, elevation profile, and debug display color.

- `TileLayoutDefinition`
  Starting tile set, draw rules, adjacency constraints, seed data, home tile placement, and rune-distance rules.

- `RegionDefinition`
  Region ID, terrain type, elevation band, danger value, landmark rules, rune eligibility, chill/stamina modifiers.

- `LandmarkDefinition`
  Landmark type, spawn rules, navigation role, visibility range, debug label, optional audio cue.

- `RuneDefinition`
  Rune type, spawn rules, cue profile, standing-stone effect, progression weight.

- `PursuerTuning`
  State timings, awareness thresholds, distance bands, pressure scaling, rune-progress modifiers.

- `PlayerStatsTuning`
  Stamina max, drain rates, recovery rates, chill rates, slope modifiers, sprint behavior.

Likely event channels or C# events:

- Player entered region.
- Rune discovered.
- Rune collected.
- Rune deposited.
- Stamina changed.
- Chill changed.
- Sprint started/stopped.
- Pursuer state changed.
- Debug overlay toggled.

Recommended folder direction:

- `Assets/LostForest/Scripts/Core`
- `Assets/LostForest/Scripts/World`
- `Assets/LostForest/Scripts/Player`
- `Assets/LostForest/Scripts/Runes`
- `Assets/LostForest/Scripts/Pursuer`
- `Assets/LostForest/Scripts/UI`
- `Assets/LostForest/Scripts/Debug`
- `Assets/LostForest/Data`
- `Assets/LostForest/Scenes`
- `Assets/LostForest/Prefabs`

## First Prototype Scope

The smallest playable Phase 2 test scene should prove one complete rune run.

Minimum scene:

- A snowy low-poly forest test area.
- Central standing stones as home.
- Hidden region grid or zone structure under the terrain.
- Several simple landmark types.
- First-person walking and sprinting.
- Stamina drain/recovery.
- Chill rising over time.
- One or more runes spawned away from home.
- Rune pickup or activation.
- Rune return and deposit at standing stones.
- Simple pursuer pressure that escalates after time, distance, or rune pickup.
- Minimal player feedback for stamina, chill, rune state, and pursuer proximity.
- Debug overlay showing hidden regions, rune location, player stats, and pursuer state.

The first successful prototype loop:

1. Player starts at standing stones.
2. Player enters forest and loses direct view of home.
3. Player uses landmarks/terrain to locate a rune.
4. Player collects the rune.
5. Pursuer pressure increases.
6. Player navigates back to standing stones.
7. Player deposits the rune.
8. Game confirms progress and resets or escalates for another rune.

## Out Of Scope For Early Phase 2

Do not prioritize these yet:

- High-fidelity art.
- Final character models.
- Full monster reveal or combat.
- Complex inventory.
- Quest systems.
- Dialogue or narrative cutscenes.
- Full procedural world generation.
- Large biome variety.
- Save/load.
- Main menu polish.
- Advanced weather simulation.
- Multiplayer.
- Full accessibility/pass settings.
- Console/controller polish beyond basic input flexibility.
- Extensive animation systems.
- Production-level audio mix.
- Final UI styling.

Early Phase 2 should stay focused on the playable 3D loop and the systems that make navigation, pressure, and return meaningful.

## Open Questions

- How literal should hidden cells be in 3D: square grid, hex-like regions, Voronoi zones, authored trigger volumes, or terrain-based regions?
- Should the first world be handcrafted, procedurally generated, or a hybrid of authored landmarks over generated region data?
- How much of the standing stones should be visible from a distance?
- Should players carry one rune at a time, or multiple?
- Does chill function mainly as a timer, a debuff system, or a navigation pressure?
- How should elevation affect movement in a first-person controller without feeling sluggish?
- What is the pursuer's earliest playable form: invisible pressure system, audio-only stalker, simple proxy object, or partial visual entity?
- Should rune locations be fully randomized, selected from authored spawn points, or generated by region rules?
- How should the game communicate "home" without a compass or explicit objective marker?
- How much UI is acceptable before it weakens the lost-in-the-forest feeling?
- What debug views are essential for tuning navigation and pressure?
- What is the target duration for a first successful rune run: 3 minutes, 5 minutes, 10 minutes?
- What causes failure in the first prototype: chill max, pursuer catch, exhaustion, lost timer, or no hard failure yet?

## Recommended First Threads

Open these as dedicated Phase 2 threads next:

1. Phase 2 Unity Architecture
   Define the initial scene, folder structure, manager responsibilities, data objects, and event flow.

2. First-Person Movement and Stats
   Build the movement controller, sprint, stamina, chill, slope modifiers, and feedback hooks.

3. Hidden Region and Terrain System
   Decide how hidden cells/regions work in 3D and how terrain communicates movement/navigation cost.

4. Rune and Standing Stones Loop
   Prototype rune spawning, discovery, pickup/activation, return, deposit, and progress feedback.

5. Landmarks and Navigation Language
   Define the first landmark set and how the player reads the world without compass UI.

6. Pursuer Pressure Prototype
   Create the first non-final pursuer state machine focused on pressure, audio, and debug visibility.

7. Debug Tools and Tuning UI
   Build the developer overlay and commands needed to tune the prototype quickly.

8. Low-Poly Forest Art Direction
   Establish practical visual rules for snowy birch/aspen terrain, silhouettes, scale, and readability.

## Immediate Next Tasks

1. Create the Unity Phase 2 project structure.
   Set up `Assets/LostForest` folders, an initial test scene, and basic assembly organization if desired.

2. Build the graybox forest test scene.
   Add simple terrain, snow material, tree proxies, standing stones, a few landmark objects, and clear scale references.

3. Implement first-person movement.
   Add walking, sprinting, ground checks, slope handling, and basic camera look.

4. Implement stamina and chill as pure systems.
   Start with visible debug values before adding atmospheric feedback.

5. Implement hidden regions.
   Use simple invisible volumes or a grid-based lookup first. Track the player's current region and expose it in debug UI.

6. Implement one rune and one return point.
   Spawn a rune in a valid region, allow pickup/activation, and deposit it at standing stones.

7. Add basic landmark placement.
   Add a small set of readable landmarks between home and rune regions to test navigation memory.

8. Add the first pursuer pressure pass.
   Start with an invisible or proxy pursuer that changes state based on time, distance, rune pickup, and player noise.

9. Add debug overlay and hotkeys.
   Show player stats, current region, rune location, standing-stone direction for development, and pursuer state.

10. Run the first full loop test.
    Confirm that the player can leave home, find a rune, feel pressure, return, deposit, and understand what happened.
