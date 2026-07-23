using UnityEngine;

namespace LostForest.Phase2.World
{
    public readonly struct TerrainElevationSample
    {
        public TerrainElevationSample(
            TerrainSurfaceSample surfaceSample,
            float logicalElevationMeters,
            float visualElevationMeters,
            TerrainElevationBand elevationBand,
            TerrainLandform landform,
            float slopeDegrees,
            float steepness01,
            Vector3 uphillDirection,
            Vector3 downhillDirection,
            int hexDistanceFromHome,
            float planarDistanceFromHomeMeters,
            float elevationDeltaFromHomeMeters,
            Vector3 planarDirectionFromHome)
        {
            SurfaceSample = surfaceSample;
            LogicalElevationMeters = logicalElevationMeters;
            VisualElevationMeters = visualElevationMeters;
            ElevationBand = elevationBand;
            Landform = landform;
            SlopeDegrees = slopeDegrees;
            Steepness01 = Mathf.Clamp01(steepness01);
            UphillDirection = FlattenDirection(uphillDirection);
            DownhillDirection = FlattenDirection(downhillDirection);
            HexDistanceFromHome = hexDistanceFromHome;
            PlanarDistanceFromHomeMeters = Mathf.Max(0f, planarDistanceFromHomeMeters);
            ElevationDeltaFromHomeMeters = elevationDeltaFromHomeMeters;
            PlanarDirectionFromHome = FlattenDirection(planarDirectionFromHome);
        }

        public TerrainSurfaceSample SurfaceSample { get; }
        public float LogicalElevationMeters { get; }
        public float VisualElevationMeters { get; }
        public TerrainElevationBand ElevationBand { get; }
        public TerrainLandform Landform { get; }
        public float SlopeDegrees { get; }
        public float Steepness01 { get; }
        public Vector3 UphillDirection { get; }
        public Vector3 DownhillDirection { get; }
        public int HexDistanceFromHome { get; }
        public float PlanarDistanceFromHomeMeters { get; }
        public float ElevationDeltaFromHomeMeters { get; }
        public Vector3 PlanarDirectionFromHome { get; }
        public bool HasHomeReference => HexDistanceFromHome >= 0;
        public bool IsAboveHome => ElevationDeltaFromHomeMeters > 0.25f;
        public bool IsBelowHome => ElevationDeltaFromHomeMeters < -0.25f;

        public static TerrainElevationSample FromSurfaceSample(
            TerrainSurfaceSample surfaceSample,
            TerrainFrameSettings settings,
            float terrainSurfaceLiftMeters,
            float homeLogicalElevationMeters,
            int hexDistanceFromHome,
            float planarDistanceFromHomeMeters,
            Vector3 planarDirectionFromHome)
        {
            float logicalElevation = surfaceSample.GetLogicalElevationMeters(settings, terrainSurfaceLiftMeters);
            float slopeDegrees = surfaceSample.SlopeDegrees;

            return new TerrainElevationSample(
                surfaceSample,
                logicalElevation,
                surfaceSample.VisualElevationMeters,
                ResolveElevationBand(logicalElevation, settings),
                TerrainFrameGenerator.GetLandformAtWorldPosition(surfaceSample.Position, settings),
                slopeDegrees,
                Mathf.Clamp01(slopeDegrees / 55f),
                surfaceSample.UphillDirection,
                surfaceSample.DownhillDirection,
                hexDistanceFromHome,
                planarDistanceFromHomeMeters,
                logicalElevation - homeLogicalElevationMeters,
                planarDirectionFromHome);
        }

        public static TerrainElevationBand ResolveElevationBand(float logicalElevationMeters, TerrainFrameSettings settings)
        {
            float amplitude = settings == null ? new TerrainFrameSettings().HeightAmplitudeMeters : settings.HeightAmplitudeMeters;

            if (amplitude <= 0.001f)
            {
                return TerrainElevationBand.Mid;
            }

            float normalized = Mathf.InverseLerp(-amplitude, amplitude, logicalElevationMeters);

            if (normalized < 0.20f)
            {
                return TerrainElevationBand.DeepLow;
            }

            if (normalized < 0.40f)
            {
                return TerrainElevationBand.Low;
            }

            if (normalized < 0.62f)
            {
                return TerrainElevationBand.Mid;
            }

            if (normalized < 0.82f)
            {
                return TerrainElevationBand.High;
            }

            return TerrainElevationBand.Ridge;
        }

        private static Vector3 FlattenDirection(Vector3 direction)
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
