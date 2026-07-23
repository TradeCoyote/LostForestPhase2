# Lost Forest Phase 2 - Elevation Design Note

## Current Generation Model

`TerrainFrameGenerator` builds Frame-owned height points for each Slot: center, six vertices, six edge midpoints, and six inner points. Every point is keyed by rounded world X/Z through `TerrainFrameData.GetPointIdFromWorldPosition(...)`, so adjacent Slots reuse the same boundary point and stitch without cracks.

Before this pass, height came from three world-space waves plus one Perlin layer. The broad waves were useful, but the local Perlin scale and point-by-point variation could make adjacent height samples feel busy inside a single tile. The renderer also generates active Slots one at a time, so the height function must stay a pure function of seed, world X/Z, and shared terrain settings.

The new model treats the hidden Field like a very coarse topographic map. Elevation is still sampled at terrain points, but the intended read is contour-scale: broad bands, long rises, channels, crests, sheltered lows, and table-like high ground. This follows the useful part of real topographic maps: contour lines describe equal-elevation shape, while contour spacing implies slope and travel pressure.

## Elevation Vocabulary

- Slopes: long directional rises/falls that let the player say "I climbed uphill from Home."
- Ridges: extended high lines that can become orientation silhouettes or landmark candidates.
- Valleys: extended low lines that bend travel language around terrain, such as "the valley curves left."
- Basins: broad low areas, especially around Home, that make departure feel like climbing out.
- Mesas: elevated, flatter tableland with softened rims, useful as memorable high ground without making a board-shaped plateau.
- High landmarks: later placement candidates on ridge crests or outer rises.
- Low corridors: later route candidates in valley channels and saddles.
- Home-region tendency: Home should sit in a mild basin or sheltered low shelf, not on noisy terrain.
- Outer-lane tendency: outer lanes should trend higher overall, with low corridors cutting through them.

## Implemented Control Layer

The height function is now a deterministic macro-landscape blend:

- Home basin bias around the configured Home world center.
- Outer rise based on distance from Home.
- Seeded directional slope across the Field.
- Seeded ridge and saddle waves with long wavelengths.
- Seeded low corridor channels.
- Seeded mesa/tableland with a broad top and softened rim.
- A low-weight, slow Perlin detail layer for natural surface variation.

The resulting height is stored as logical elevation meters on `SharedHeightPoint.Height`. The rendered Y position is still `logical elevation * visualHeightMultiplier`, so gameplay handoffs should use logical meters and rendering can exaggerate or soften the visible terrain separately.

`TerrainFrameGenerator.GetLogicalHeightAtWorldPosition(...)` is the deterministic height source. `TerrainFrameGenerator.GetLandformAtWorldPosition(...)` resolves the same macro profile into a simple landform label for downstream systems.

## Recommended Defaults

- `heightAmplitudeMeters`: `42`
- `visualHeightMultiplier`: `1.35`
- `broadHeightScale`: `0.0034`
- `noiseHeightScale`: `0.0022`

These values make the dominant changes happen over several tiles instead of inside one tile. `broadHeightScale` gives long waves across roughly twenty flat-to-flat tile widths, while `noiseHeightScale` produces very slow surface variation with low influence. `heightAmplitudeMeters` is higher than the previous `34` so macro climbs and descents are more legible after local jitter is reduced.

## Handoff Contract

Later systems should query terrain through the World layer and consume numeric values, not regenerate height. The current handoff exposes:

- Current logical elevation.
- Visual elevation.
- Slope angle and steepness.
- Uphill and downhill planar directions.
- Elevation band.
- Landform label: HomeBasin, LowCorridor, Valley, LongSlope, RollingGround, Saddle, RidgeLine, HighGround, Mesa.
- Hex distance from Home.
- Planar distance from Home.
- Elevation delta from Home.

Do not implement stamina, rune placement, pursuer decisions, or fog rules directly in this layer. Those systems should read these values later.

Tiles should behave as content recipes sitting on the generated landform. Later tile rules can ask for `allowedElevationBands`, preferred landforms, slope limits, or distance-from-Home requirements, but a Tile must not author or override terrain height.

## Geography References

- USGS topographic maps: contours connect points of equal elevation and communicate terrain shape and slope.
- National Geographic mesa definition: a mesa is a wide, flat, elevated landform with steep sides. Phase 2 uses a softened version for walkable low-poly terrain.

## Validation

Use the Grid Movement Fog test scene:

`Lost Forest > Bootstrap > Create or Repair Grid Movement Fog Test Scene`

Then run:

`Lost Forest > Bootstrap > Validate Grid Movement Fog Test Scene`

Expected result: the canonical `26 x 26` Field initializes, Home renders in the active radius-1 window, the player resolves to Home after spawning, terrain samples ground the player and content, and shared boundary point reuse remains intact.
