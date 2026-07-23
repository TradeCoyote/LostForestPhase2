# Lost Forest Phase 2 - Player-Centered Fog Visibility Spine

## Purpose

Fog and visibility are core to Lost Forest Phase 2. They are not only visual polish and they should not be treated as a late atmosphere pass.

The hidden hex Field remains the authoritative board-game layer for Slot identity, Tile assignment, adjacency, rune logic, home distance, pursuer pressure, and debug reporting. The player-facing forest, however, must feel seamless. A player should not step over an invisible Slot boundary and see a whole new Tile or Field suddenly appear on the horizon.

This document defines the Phase 2 rule:

Hidden Tiles decide what exists. Player-centered fog decides what the player can perceive.

## Core Principle

Visibility is based on distance from the player, not distance from a Tile boundary.

The active render system may load, build, pool, or remove terrain and content by hidden Slot or chunk. That is allowed and expected. But the player should experience a continuous visibility shell around their body and camera.

Player-facing presentation should be controlled by:

- Distance from player.
- Local fog density.
- Line of sight and terrain occlusion later.
- Tree and landmark silhouette priority.
- Weather/fog drift.
- Pursuer, rune, chill, or region modifiers later.

It should not be controlled directly by:

- Crossing a hex edge.
- Entering a new Slot.
- A full Tile becoming visible all at once.
- Debug active radius changes.

## Layer Separation

### Hidden Field Layer

Owns:

- Frame, Slot, Tile, and Field data.
- Slot address and axial coordinate.
- Tile ID and orientation.
- Terrain ownership and shared height points.
- Home, rune, threat, and region state.
- Pursuer route logic.
- Debug reporting.

The player never sees this layer unless debug mode is enabled.

### Active World Layer

Owns:

- Which nearby Slots are generated, loaded, pooled, or retained.
- Terrain and collision availability around the player.
- Placeholder content spawning from Tile recipes.
- Content lifetime and cleanup.
- Preloading outside the visible distance so presentation has time to hide construction.

This layer may use hidden Slot radius because it is a technical loading system.

### Perception Layer

Owns:

- What the player can actually see.
- How trees, stones, runes, terrain details, and distant silhouettes resolve.
- Fog density, fog color, and whiteout.
- Detail bands and fade curves.
- Weather-like motion and uneven fog pockets.

This layer must be player-distance based and visually seamless.

## Recommended Visibility Bands

Exact values are prototype tuning knobs, not canon final numbers.

- Near Definition Band: about `0-25m`.
  Trees, stones, runes, tracks, and terrain details are readable. Materials show normal color and shape.

- Mid Recognition Band: about `25-50m`.
  Major forms are readable. Tree trunks, large stones, and landmarks still have shape, but smaller details are muted.

- Fog Silhouette Band: about `50-80m`.
  Trees and large landmarks become pale silhouettes in fog. Objects should be low contrast, simplified, and partially swallowed by white.

- Whiteout / Hidden Band: beyond about `80m`.
  The forest is not meaningfully readable. Terrain and content may be loaded for technical reasons, but should not be visually clear.

These bands should be exposed as inspector settings for early tuning.

## Reveal Behavior

Objects should not simply pop from invisible to fully detailed.

Early target behavior:

1. Far trees appear as soft white or gray silhouettes inside fog.
2. As the player approaches, silhouette opacity and form increase.
3. Trunk/crown separation becomes visible.
4. Local color, bark tone, snow caps, and geometry definition resolve near the player.

The reverse should also work when the player moves away:

1. Detail drains out.
2. Color desaturates toward fog color.
3. Shape becomes a silhouette.
4. The object disappears into whiteout before it is unloaded.

The active render system may instantiate a whole Slot behind the scenes, but every spawned content object should enter through the perception layer.

## Birch Fog Readability Reference

The current visual target is a snowy birch/aspen forest where nearby trunks are tall, pale, vertical anchors and distant trunks dissolve into white fog.

Prototype trees should support this test even before final art exists:

- Trunks should be much taller than the first placeholder tree scale, about `3x` the earlier prototype range.
- The extra height leaves room for a later fog ceiling that can sit above the ground at varying heights.
- Pale trunks should carry small dark bark bands at varied heights.
- For the early prototype, these marks may use one fixed reusable tree pattern rather than unique randomized bark on every tree.
- These black marks should be least readable in thick fog.
- The marks should become more clear as fog thins or as the player gets closer.
- The test is successful when distant trees read as pale silhouettes and near trees resolve into visible dark bark detail.

The bark bands are not final birch art. They are a prototype readability instrument for tuning distance fog, silhouette fade, and detail reveal.

## Living Fog

The fog should feel like a living environment, not a static camera mask.

Prototype fog can begin simply, but should be designed to grow into:

- World-space density noise.
- Slow drift over time.
- Thicker and thinner pockets.
- Wind direction and speed.
- Local fog bias from terrain hollows, ridges, clearings, danger regions, or rune sites.
- Pressure changes when the pursuer is close.
- Chill-state modifiers if needed.

Fog behavior should sample world position and time. It should not snap when the player crosses Slot boundaries.

Early implementation can use a cheap approximation:

- Unity global fog for broad distance falloff.
- A `FogVisibilityDirector` controlling prototype fog color, distance, and density.
- Per-object reveal materials driven by player distance.
- Optional simple fog planes, billboards, or particles later if performance allows.

Do not require high-end volumetric fog in the first pass.

Laptop-performance rule:

Until core gameplay pressure is further along, use cheap built-in distance fog and simple prototype reveal tests. Do not spend the next milestone chasing expensive volumetric fog, particle-heavy fog banks, or custom shader complexity.

## Streaming And Preload Rule

The active Slot render radius should be larger than the visible clarity radius.

Reason:

If a Slot is only created when it becomes visible, the player will notice construction pop. Instead, nearby Slots should be generated before they are clearly visible, then hidden inside the fog/silhouette band until the player gets closer.

Recommended early model:

- Player current Slot is resolved through hidden Field tracking.
- Active build radius ensures terrain and content exist beyond the visible range.
- Perception radius controls what can be seen.
- Unload radius is larger than active build radius or uses hysteresis so objects do not flicker when the player moves near a threshold.

This creates three different distances:

- Simulation or Field radius: what the hidden systems know about.
- Build or preload radius: what is instantiated around the player.
- Perception radius: what the player can actually see.

Only the perception radius should be player-facing.

## Relationship To Hex Tiles

Hex Tiles remain essential, but they are hidden structure.

Tiles should drive:

- Which content recipe is spawned.
- Tile ID and orientation.
- Rune eligibility.
- Landmark logic.
- Danger/fog/audio tags.
- Pursuer and route pressure.
- Debug identity.

Tiles should not drive:

- A visible hard edge in the forest.
- Whole-region pop-in.
- Player-facing grid shape.
- Abrupt visibility changes at Slot borders.

The player may eventually infer that the forest has repeated structural logic, but should not feel like they are walking across visible board spaces.

## Early Prototype Acceptance Criteria

A first fog/reveal prototype should pass these checks:

- Walking across a hidden Slot edge does not reveal an entire new Tile at once.
- Trees at the edge of visibility appear first as pale fog silhouettes.
- Closer trees resolve into more defined placeholder low-poly forms.
- The visible radius follows the player smoothly.
- Debug mode can show loaded Slots, current Slot, and active render radius.
- Player-facing mode hides Slot labels, Tile IDs, hex outlines, and construction markers.
- Fog density can vary over time or by sampled world position, even if the first implementation is simple.
- The active render radius can be larger than the visible radius.
- Unloading happens after content is hidden by fog, not while it is clearly visible.

## Recommended First Implementation Slice

Do not start with final volumetric fog.

Start with a thin, testable spine:

1. Add a `PlayerVisibilitySource` that exposes player position and view/fog settings.
2. Add a `FogVisibilityDirector` that owns global fog values and prototype tuning bands.
3. Add a `DistanceRevealDriver` for placeholder trees and landmarks.
4. Update active Slot rendering so newly spawned objects begin in the far/fog state.
5. Add a debug HUD or console report for current Slot, active Slots, visible range, and fog band values.
6. Create a dedicated fog/reveal test scene using the current first-person walk setup.

This proves the feel before investing in heavier fog rendering technology.

## Deferred

Hold these for later:

- Final volumetric fog package decisions.
- Heavy particle fog fields.
- Production tree shaders.
- Full weather simulation.
- Pursuer-driven fog manipulation.
- Rune-specific fog effects.
- Large 26 x 26 performance optimization.
- Phase 3 art-quality fog.

## Open Questions

- What should the first playable visibility radius feel like in meters?
- Should silhouettes be mostly trees, or also rocks and landmarks?
- Should home stones remain readable at a longer distance than ordinary trees?
- Should fog be thicker in hollows and thinner on ridges?
- How much should fog respond to pursuer pressure before the pursuer is actually implemented?
- Should rune sites create tiny local clarity or distortion?
- What is the minimum loaded Slot radius needed so the player never catches pop-in?
