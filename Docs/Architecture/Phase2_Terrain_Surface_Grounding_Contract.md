# Lost Forest Phase 2 - Terrain Surface Grounding Contract

## Purpose

Tile content must conform to Frame-owned terrain without owning, rewriting, rotating, or regenerating terrain. This contract applies to placeholder trees and Home standing stones now, and later to fallen logs, cairns, shrines, rune objects, landmark props, and high-quality Phase 3 forest assets.

## Rule

Frame owns terrain height, mesh, stitching, collision, and region identity.

Tiles own content recipes.

Tile content may query terrain through a shared World-layer surface sampler. Tile content must not calculate its own final placement height.

## Runtime Helper

Use `TerrainSurfaceSampler` for content placement.

Sampling order:

1. Raycast down against registered generated terrain MeshColliders only.
2. If raycast fails, sample Frame-owned `TerrainFrameData` / `TerrainSlotData` height points using the same slot surface topology as the generated terrain mesh.
3. If both fail, skip placement and report it.

The sampler reports:

- Raycast sample count.
- Frame fallback sample count.
- Failed sample count.

## Current Integration

`TileContentSpawner` uses the sampler for each placeholder tree base. X/Z placement stays deterministic from the tile recipe seed; only Y/normal grounding comes from the terrain surface query.

`HomeLandmarkBuilder` uses the sampler for each standing stone local offset. Each stone gets a grounded root at its sampled surface point, then the primitive cylinder is placed as a child from that root with a small downward embed.

`SevenHexTerrainFrameDebugView` exposes a sampler from its current `TerrainFrameData` and generated terrain mesh data, but the sampler itself is not specific to the 7-hex proof.

## Do Not

- Do not place content using only Slot center height.
- Do not let trees, stones, runes, or landmarks invent separate height logic.
- Do not raycast against spawned content colliders.
- Do not hand-adjust scene objects to solve grounding.
- Do not make Tile content own terrain height.
