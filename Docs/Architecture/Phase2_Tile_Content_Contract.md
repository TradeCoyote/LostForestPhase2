# Lost Forest Phase 2 - Tile Content Contract

## Purpose

This contract defines what a Lost Forest Tile owns as content, how that content stays separate from Frame-owned terrain, and how the Phase 2 prototype can grow into a high-quality Phase 3 winter forest without needing 999 handmade heavy scenes.

Phase 2 may still use simple placeholder cylinders, cones, stones, and debug markers. The data boundary should already assume later high-quality trees, snow dressing, landmarks, fog, audio, LOD, pooling, and streaming.

## Non-Negotiable Boundary

Frame owns terrain.

Frame and Slot data own:

- Slot address and axial coordinate.
- World position.
- Shared terrain height points.
- Terrain mesh generation.
- Terrain stitching.
- Walkable terrain polygon.
- Terrain collision.
- Neighbor graph and reachability.
- Region membership and player current-region tracking.

Tile owns content.

Tile data owns:

- Content identity and category.
- Forest fill recipe.
- Landmark recipe, if any.
- Clearing and tree-exclusion rules.
- Content anchors and sockets.
- Rune support metadata.
- Navigation readability metadata.
- Danger and atmosphere tags.
- Render budget and LOD intent.
- Deterministic content seed salt.

Terrain does not rotate. Tile content may rotate with the Tile orientation.

The important implementation rule is simple: a `TileInstance` may rotate local content positions around the Slot center, but it must not rotate or rewrite Frame terrain, Slot coordinates, shared edge heights, collision, or adjacency.

## Core Model

Each Tile should be a content recipe, not a fully unique scene.

The recipe can spawn placeholder content now and high-quality assets later. The same Tile ID should rebuild the same content from the same Field seed, Slot address, and orientation.

Recommended content layers:

- Terrain reference layer: reads Frame-owned terrain and walkable bounds.
- Forest fill layer: deterministic trees, brush, snow mounds, stumps, deadfall, and local clutter.
- Clearing layer: subtracts or thins forest fill around important anchors, approaches, and exits.
- Landmark layer: authored, procedural, or hybrid feature such as a cairn, hollow log, shrine, fallen tree, stone ring, split tree, creek bend, or unusual stump field.
- Silhouette layer: cheap distant dark tree shapes or billboards used at the fog boundary.
- Gameplay layer: rune eligibility, navigation tags, danger tags, threat-path tags, home-adjacency rules, and tuning values.
- Audio/atmosphere layer: wind tone, branch creaks, snow crunch modifiers, distant calls, landmark loops, and pursuer-pressure hooks.
- Rendering layer: LOD profile, material set, instance budget, collider budget, pooling key, and optional Addressables key later.

## Tile Categories

Use four production categories plus one development/reserve category.

Primary categories:

- `Home`: Tile `000`; standing-stone clearing and player-start anchor.
- `Forest`: ordinary forest recipes, including dense forest, sparse forest, ridge, hollow, creek, boulder field, dead grove, and transition variants.
- `Landmark`: landmark-led recipes where a major or medium landmark defines the tile's navigation value.
- `ThreatOrigin`: Tile `666`; pursuer or pressure-origin tile.
- `Reserved`: fallback, debug, future production expansion, and migration space.

Do not make every capability a category. Rune support, home-adjacent permission, threat-path permission, density, danger, visibility, and biome tone should be fields or tags. A landmark tile may also be rune-eligible. A forest tile may also be high danger. A threat-influenced tile is still a forest tile unless it is the unique `ThreatOrigin`.

Code should never infer behavior from numeric ranges. Numeric ranges are for human organization only; systems should read data fields and tags.

## Recommended Tile Bank Breakdown

The canonical bank is `000-999`.

Recommended organization:

- `000`: Home / player-start Tile.
- `001-599`: Ordinary forest recipes. These carry the bulk of replay variety through density, topography affinity, species mix, visibility, danger, and audio tone.
- `600-649`: Rune-supporting ordinary forest recipes. These are still forest tiles, but they have stronger rune sockets, clearing shapes, and discovery readability.
- `650-665` and `667-700`: Threat-influenced forest recipes. These support pressure routes, darker silhouettes, dead groves, broken branches, and higher danger tuning. Tile `666` is skipped because it is reserved.
- `666`: Pursuer / threat-origin Tile.
- `701-800`: Landmark-capable or landmark-authored recipes. This gives about 100 landmark tiles without making landmarks dominate the forest.
- `801-899`: Future special terrain, seasonal variants, transition recipes, and production expansion.
- `900-999`: Reserve, debug, fallback, migration, and test recipes.

Rune eligibility should also be allowed inside the landmark set. A good Phase 3 target is roughly 50 dedicated rune-supporting forest recipes plus 15-25 rune-capable landmark recipes, then runtime filtering chooses the final rune locations.

## Data Every Tile Should Carry

Every static `TileDefinition` should eventually contain these field groups.

Identity:

- `tileId`
- `debugName`
- `contentContractVersion`
- `contentCategory`
- `reservedRole`
- `isUniqueTile`
- `spawnWeight`
- `seedSalt`
- `tags`

Frame compatibility:

- `allowedTerrainTags`
- `allowedElevationBands`
- `slopeTolerance`
- `edgeAffinityTags`
- `requiresWalkableCenter`
- `requiresOpenEdges`
- `forbiddenSlotRoles`
- `placementPriority`

Content rotation:

- `contentSupportsRotation`
- `allowedOrientationSteps`
- `rotatedAnchorPolicy`
- `rotatedApproachPolicy`
- `nonRotatingSocketIds`

Forest fill:

- `forestDensity`
- `treeSpacingMin`
- `treeSpacingMax`
- `maxTreeInstancesCurrent`
- `maxTreeInstancesAdjacent`
- `maxSilhouetteInstances`
- `speciesMix`
- `treeScaleRange`
- `treeLeanRange`
- `deadfallRate`
- `stumpRate`
- `brushDensity`
- `snowMoundDensity`
- `canopyOpenness`
- `edgeScreeningBias`
- `pathOpeningBias`

Clearing:

- `centerClearingRadius`
- `landmarkClearingRadius`
- `edgeExitClearance`
- `treeExclusionZones`
- `thinForestZones`
- `requiredApproachCorridors`

Landmark:

- `landmarkType`
- `landmarkAuthoringMode`
- `landmarkAnchorId`
- `landmarkLocalPosition`
- `landmarkFacingDegrees`
- `landmarkFootprintRadius`
- `landmarkClearanceRadius`
- `landmarkVisibilityRange`
- `landmarkSilhouettePriority`
- `landmarkUniquenessGroup`
- `landmarkPrefabKey`
- `landmarkDressingRecipe`

Gameplay:

- `runeEligible`
- `runeSocketIds`
- `minDistanceFromHomeForRune`
- `navigationMeaning`
- `visibilityValue`
- `dangerValue`
- `chillModifier`
- `staminaModifier`
- `noiseModifier`
- `homeAdjacentAllowed`
- `threatPathAllowed`
- `blocksPursuerLine`

Atmosphere:

- `windTone`
- `branchCreakProfile`
- `snowCrunchModifier`
- `distantAnimalCueWeight`
- `pursuerCueAffinity`
- `fogDensityBias`
- `colorTemperatureBias`

Rendering:

- `lodProfile`
- `materialSet`
- `poolingGroup`
- `colliderProfile`
- `currentTileBudget`
- `adjacentTileBudget`
- `fogBoundaryBudget`
- `addressableGroupKey`

## Runtime Tile Instance Data

`TileInstance` should stay runtime-specific. It should not duplicate all authored recipe data.

Recommended runtime fields:

- `instanceId`
- `tileId`
- `slotAddress`
- `rowIndex`
- `columnIndex`
- `axialCoordinate`
- `worldPosition`
- `orientationIndex`
- `contentSeed`
- `neighborRefs`
- `distanceFromHome`
- `role`
- `currentRegionId`
- `selectedVariantIds`
- `selectedLandmarkSocket`
- `selectedRuneSocket`
- `runtimeLodState`
- `streamingState`
- `spawnedContentHandles`
- `runtimeFlags`

The current code already has the right foundation: `TileDefinition` is static identity and `TileInstance` is a placed assignment. The next step is not to replace that boundary, but to add content-profile fields around it.

## Ordinary Forest Tile Contract

Ordinary forest tiles are not empty filler. They are the main fabric of the game.

Each ordinary forest tile should define:

- Density: sparse, normal, dense, thicket, or screen-heavy.
- Spacing: minimum and preferred tree distance, tuned per visual style and performance budget.
- Species mix: birch, aspen, pine, dead trunk, sapling, stump, and future snow-loaded variants.
- Sightlines: how much the player can see across the tile and through exits.
- Edge behavior: whether trees screen edges, open routes, or imply direction.
- Center behavior: whether the center is open, cluttered, blocked, or landmark-ready.
- Micro-landmarks: local memory cues such as two leaning trees, stump pair, broken branch, exposed root, or small stone.
- Surface dressing: snow mounds, brush, rocks, sticks, footprints later.
- Gameplay tuning: danger, chill, stamina, noise, rune support, and home/threat constraints.
- Render budget: how much of the recipe appears in current, adjacent, and fog-boundary states.

Dense forest should not simply mean "spawn more expensive trees." It should mean a combination of trunk placement, silhouettes, limited sightlines, audio occlusion, darker material bias, and fog. The player should feel density even when the engine is drawing a controlled number of instances.

## Landmark Tile Contract

Landmark tiles should be hybrid recipes by default.

Use an authored core for the landmark itself, then procedural dressing around it:

- Authored core: the recognizable object or arrangement.
- Procedural dressing: nearby trees, clearing edge, snow, small stones, fallen branches, and silhouette support.
- Data constraints: where the landmark can appear, how it is approached, and whether it can support runes or pressure.

Fully authored unique chunks should be rare. Reserve them for Home, the threat origin, and a small number of future story-critical or production-critical set pieces. Avoid building 999 bespoke chunk scenes.

Each landmark tile should define:

- `landmarkType`: hollow log, fallen birch, cairn, shrine, split tree, carved tree cluster, stone ring, frozen creek bend, broken fence remnant, animal bones, unusual stump field, or similar.
- `authoringMode`: authored core, procedural recipe, or hybrid.
- `anchorPosition`: local Tile-space anchor, usually an existing content anchor.
- `facingDirection`: local facing that rotates with Tile orientation when allowed.
- `footprintRadius`: hard occupied area.
- `clearingRadius`: soft readability area.
- `treeExclusionZone`: no-tree zone around the landmark and approach.
- `requiredVisibilityCone`: one or more approach cones that must stay readable.
- `readabilityDistance`: distance at which the landmark should become identifiable as a silhouette or shape.
- `surroundingPattern`: optional ring trees, paired trunks, corridor opening, stump field, or boulder scatter.
- `navigationMeaning`: homeward cue, route split, warning, reward clue, edge cue, or memory anchor.
- `runeSupport`: none, possible, preferred, or required.
- `homeAdjacentAllowed`
- `threatPathAllowed`
- `uniquenessGroup`
- `minSpacingFromSameGroup`
- `fallbackLandmarkType`

Landmark readability is part of the contract. A landmark that spawns behind dense trees, inside an impossible slope, or with no readable approach has failed even if the prefab technically exists.

## Landmark Placement Validation

The layout/content builder should reject or adjust impossible landmark placements.

Validation checks:

- The landmark footprint fits inside the Slot walkable polygon.
- The anchor sits on an allowed terrain slope and elevation band.
- At least one approach corridor connects from a valid edge or inner anchor.
- The clearing radius does not erase required terrain meaning.
- Tree exclusion removes blockers from the approach and landmark silhouette.
- The visibility cone remains at least partially readable from the intended direction.
- The landmark does not block required navigation between open edges.
- Home-adjacent and threat-path restrictions are satisfied.
- Rune sockets are not on Home, unreachable terrain, or conflicting collision.
- Same uniqueness group landmarks are not clustered unless explicitly allowed.
- Required special tiles are reachable from Home.

If validation fails, the builder should try another orientation, then another compatible Slot, then the declared fallback recipe. It should report the failure in debug output rather than silently spawning an unreadable landmark.

## Tree Density, Clearings, And Visibility

Tree placement should be deterministic and mask-driven.

Use the Field seed, Tile ID, Slot address, and orientation index to derive a stable content seed. From that seed, scatter trees inside the Frame-provided walkable polygon. Apply masks in this order:

1. Frame walkable bounds.
2. Hard collision exclusions.
3. Landmark footprint exclusions.
4. Clearing masks.
5. Approach corridor masks.
6. Edge-exit masks.
7. Density and species noise.
8. LOD budget reduction.

Define density in terms of both count and readability:

- Sparse: open route reading, strong silhouettes, fewer trunks.
- Normal: navigable forest with intermittent occlusion.
- Dense: short sightlines, stronger edge screening, more silhouette layers.
- Thicket: visual barrier or pressure cue, used carefully near routes.
- Clearing: reduced tree count with strong edge definition.

Clearings should not look like empty circles. They should have shaped edges, leaning trees, stones, stumps, and snow mounds. The purpose is to reveal important content and give the player a memory anchor, not to flatten the forest.

## Orientation Rules

Tile orientation affects Tile content only.

Rotation rules:

- Orientation index `0-5` maps to `0`, `60`, `120`, `180`, `240`, and `300` degrees.
- Content anchors rotate around the Slot center when `contentSupportsRotation` is true.
- Landmark facing, approach cones, tree-exclusion zones, and rune sockets rotate with the content unless explicitly marked as world-fixed.
- Edge and corner anchor indices rotate with the content recipe.
- Frame terrain, shared heights, collision, axial coordinates, and Slot world position do not rotate.
- Non-rotatable content either uses orientation `0` or fails validation for other orientations.
- Asymmetric landmarks must declare which edge/approach constraints rotate with them.

This lets the same tile recipe feel different across runs while preserving the terrain contract.

## Fog And Silhouette Rendering Strategy

Fog is both mood and performance boundary.

The intended player read:

- Current region: clear enough to move, inspect landmarks, and make immediate route decisions.
- Adjacent regions: partially readable through trees and fog.
- Fog boundary: darker tree silhouettes, uncertain shapes, sound, and motion hints.
- Beyond fog: not visually trustworthy and not fully rendered.

Recommended detail rings:

- Ring 0, current tile: high-detail content, interactive landmarks, full local collision, nearest LODs, local audio.
- Ring 1, adjacent tiles: medium-detail forest and landmark silhouettes, reduced clutter, simplified collision where needed.
- Ring 2, fog boundary: silhouette layer, billboards, impostors, dark trunks, no fine clutter, no gameplay colliders except pinned exceptions.
- Ring 3 and beyond: unloaded, pooled, disabled, or represented only by global fog/sky treatment.

Pinned exceptions:

- Home may keep a special distant silhouette, audio cue, or faint light treatment when design wants a return anchor.
- Active rune tile may keep limited cue logic even if art is simplified.
- Pursuer-relevant tiles may keep pressure state even when their presentation is unloaded.

Fog tuning should be expressed in tile-relative terms first, then converted to meters:

- Clear range should cover most of the current tile.
- Fade should begin before adjacent tile content becomes too detailed to afford.
- Heavy fog should hide Ring 2 simplification before the player can inspect it.
- Unload distance should sit behind the fog wall, with crossfade or pooled replacement to avoid popping.

Distant silhouettes should be their own cheap recipe, not full tree fields seen through a gray overlay.

## Phase 3 Performance Strategy

Performance should come from data boundaries, not from hoping final assets are cheap.

Required strategy:

- Deterministic procedural placement from seed so content can be rebuilt instead of stored as unique scenes.
- Object pooling by content type, LOD state, material set, and collider profile.
- GPU instancing for repeated trees, trunks, brush, stones, snow mounds, and simple props.
- Shared material sets and atlased textures; avoid one material per tree or landmark variant.
- LOD groups for major assets and impostor or billboard versions for distant trees.
- Separate visual density from physics density; most trees do not need expensive colliders.
- Current and adjacent tiles get collision; fog-boundary silhouettes usually do not.
- Audio emitters are pooled and budgeted like visuals.
- Landmark prefabs are authored as reusable cores with variant dressing, not unique full chunks.
- Streaming state never changes Tile identity, rune eligibility, danger, or reachability.
- Debug overlays must show active, pooled, simplified, and unloaded content states.

Content budgets should live in data and be tuned later. Phase 2 should start with conservative counts and visible debug reporting rather than premature heavy optimization.

## Phase 2 Placeholder Implementation Recommendation

For the current low-poly prototype, implement only the content contract's first slice.

Build:

- A lightweight content profile attached to or referenced by `TileDefinition`.
- A `TileContentCategory` enum with `Home`, `Forest`, `Landmark`, `ThreatOrigin`, and `Reserved`.
- A `ForestFillProfile` with density, spacing, tree count budget, and seed salt.
- A `ClearingProfile` with center radius, landmark radius, and edge-exit clearance.
- A `LandmarkProfile` with type, anchor, facing, clearing radius, visibility cone, rune support, and placement constraints.
- A `TileRenderProfile` with current, adjacent, and fog-boundary budgets.
- A deterministic `TileContentSpawner` that can spawn primitive placeholder trees and one primitive landmark core.
- Debug labels for tile ID, content category, density, landmark type, and LOD ring.

Use simple placeholder assets:

- Trees: cylinder trunk plus cone or low-poly crown, or bare winter trunk variants.
- Dense forest: more trunks near edges plus darker silhouettes at the fog boundary.
- Clearings: fewer trunks, edge screening, visible approach corridors.
- Landmarks: primitive hollow log, cairn stack, fallen trunk, stone ring, split tree, or stump field.
- Home: existing standing-stone placeholder remains the special Home landmark.
- Threat origin: debug-only marker, dark dead grove proxy, or audio/pressure origin.

Do not implement final streaming yet. Simulate detail rings first by switching content groups based on player current region: current, adjacent, fog boundary, and hidden. This proves the contract before building a full streaming system.

## What Not To Build Yet

Do not build these in the next pass:

- 999 handmade chunk scenes.
- Final tree models.
- Final landmark models.
- Full Addressables or production streaming.
- Infinite terrain generation.
- Final fog and lighting tune.
- Final pursuer behavior.
- Runes as a complete objective system.
- Final audio implementation.
- Complex biome system.
- Save/load.
- Player-facing compass, map, tile labels, or board UI.
- High-poly collision for every tree.
- A large `26 x 26` instantiated world before the 7-hex or 19-tile content loop is readable.

## Acceptance Criteria For This Contract

The design is successful if:

- Frame-owned terrain authority remains intact.
- Tile content can rotate without rotating terrain.
- Ordinary forests, landmark tiles, Home, and threat origin use the same underlying Tile/Slot boundary.
- Dense forest can be expressed through recipes, masks, fog, silhouettes, and LOD rather than raw object count.
- About 100 landmark tiles can exist without requiring 100 heavy unique scenes.
- Rune support is data-driven and can overlap ordinary forest or landmark categories.
- Fog provides a diegetic visibility limit and a rendering budget boundary.
- Phase 2 can implement the first slice with placeholder primitive content.
- Phase 3 can swap in high-quality assets without rewriting the core Tile contract.

## Recommended Next Coding Thread

Next thread:

`Phase 2 Tile Content Placeholder Forest Thread`

Mission:

Implement the first code slice of this content contract in `/Users/klove/Documents/LostForestPhase2-home-landmark-clean`.

Suggested scope:

- Extend the existing `TileDefinition` model with content category and lightweight content profiles.
- Keep the existing `TileInstance` placement and content-anchor rotation behavior.
- Add a deterministic placeholder `TileContentSpawner`.
- Spawn primitive winter tree placeholders for ordinary forest tiles.
- Spawn one or two primitive landmark placeholders from data.
- Apply clearing and tree-exclusion masks around Home and landmarks.
- Add debug output showing tile category, density, landmark type, and content seed.
- Demonstrate that rotating Tile content changes content placement while Frame terrain remains fixed.

Suggested acceptance:

- The same seed generates the same placeholder forest content.
- Tile `000` remains a readable Home clearing with standing stones.
- Tile `666` remains a threat-origin data role and can be visualized in debug only.
- Ordinary forest tiles spawn deterministic placeholder trees.
- At least one landmark tile spawns a readable landmark from an anchor.
- Tree clearing around landmarks prevents unreadable placement.
- Current/adjacent/fog-boundary states can be visualized or toggled for testing.
