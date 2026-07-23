# Lost Forest Phase 2 - Elevation-Aware Player Speed

## Purpose

Player movement speed is the first gameplay-facing consumer of Phase 2 elevation. The goal is to make route choice physically legible: climbing should feel slower, descending should feel faster, and flat/cross-slope movement should preserve the existing first-person controller feel.

This layer does not implement stamina, chill, rune, pursuer, fog, or terrain-generation changes.

## Runtime Shape

`PlayerTerrainMovementState` lives on the player and reads terrain through the existing World layer:

- `PlayerGridAddressTracker.CurrentSlot`
- `ActiveRegionRenderer.TrySampleTerrainElevation(...)`
- `TerrainElevationSample.SlopeDegrees`
- `TerrainElevationSample.UphillDirection`

The first-person controller chooses the base movement speed first:

- walk speed
- sprint speed

Then `PlayerTerrainMovementState` applies one slope multiplier to that base speed.

## Directional Grade

Terrain slope alone is not enough. A steep hillside should not slow the player equally in every direction.

The movement layer computes directional grade by comparing movement intent against terrain uphill direction:

- movement aligned with uphill direction = uphill
- movement aligned with downhill direction = downhill
- movement mostly across the slope = flat/neutral

The signed movement grade is reported in degrees:

- positive = uphill movement
- negative = downhill movement
- near zero = flat or cross-slope movement

## Starter Tuning

Current recommended defaults:

- flat threshold: `3` degrees directional grade
- steep threshold: `30` degrees directional grade
- max uphill slowdown: `0.50x`
- max walking downhill boost: `1.65x`
- max sprint downhill boost: `1.45x`
- absolute speed multiplier clamp: `0.50x` to `1.75x`
- multiplier smoothing speed: `8` per second

At `30` degrees of directional uphill grade, walking reaches about half speed. At the same directional downhill grade, walking reaches about `1.65x` speed. Sprinting uses the same terrain read, but downhill sprint boost is capped lower so Space+downhill feels fast without becoming chaotic.

## Handoff Values

Future stamina/chill work should consume the exposed movement facts instead of recalculating terrain:

- `HasTerrainSample`
- `HasMovementIntent`
- `MovementIntentWorldDirection`
- `MovementIntentMagnitude01`
- `CurrentSlopeDegrees`
- `SignedMovementGradeDegrees`
- `SignedMovementGradeNormalized`
- `TravelState`
- `SpeedMultiplier`
- `TargetSpeedMultiplier`
- `BaseMovementSpeedMetersPerSecond`
- `FinalMovementSpeedMetersPerSecond`
- `WantsSprint`
- `IsSprinting`

Stamina and chill rates are intentionally unchanged in this pass.
