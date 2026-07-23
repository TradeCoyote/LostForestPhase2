using UnityEngine;

namespace LostForest.Phase2.World
{
    public readonly struct TerrainSurfaceSample
    {
        public TerrainSurfaceSample(Vector3 position, Vector3 normal, TerrainSurfaceSampleSource source, TerrainSlotData slot)
        {
            Position = position;
            Normal = normal.sqrMagnitude <= 0.0001f ? Vector3.up : normal.normalized;
            Source = source;
            Slot = slot;
        }

        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public TerrainSurfaceSampleSource Source { get; }
        public TerrainSlotData Slot { get; }
        public float VisualElevationMeters => Position.y;
        public float SlopeDegrees => Vector3.Angle(Normal, Vector3.up);
        public float Steepness01 => Mathf.Clamp01(SlopeDegrees / 55f);
        public Vector3 UphillDirection => -DownhillDirection;
        public Vector3 DownhillDirection => GetPlanarSlopeDirection(Vector3.ProjectOnPlane(Vector3.down, Normal));

        public float GetLogicalElevationMeters(TerrainFrameSettings settings, float terrainSurfaceLiftMeters = 0f)
        {
            float multiplier = settings == null ? 1f : Mathf.Max(0.0001f, settings.VisualHeightMultiplier);
            float appliedSurfaceLift = Source == TerrainSurfaceSampleSource.TerrainCollider ? terrainSurfaceLiftMeters : 0f;
            return (VisualElevationMeters - appliedSurfaceLift) / multiplier;
        }

        private static Vector3 GetPlanarSlopeDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }
    }
}
