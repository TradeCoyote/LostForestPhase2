using System;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class TerrainFrameSettings
    {
        private const float SqrtThree = 1.7320508f;

        [SerializeField] private float hexFlatToFlatMeters = 100f;
        [SerializeField] private int heightSeed = 4242;
        [SerializeField] private float heightAmplitudeMeters = 42f;
        [SerializeField] private float visualHeightMultiplier = 1.35f;
        [SerializeField] private float broadHeightScale = 0.0034f;
        [SerializeField] private float noiseHeightScale = 0.0022f;
        [SerializeField, Range(0f, 1f)] private float extremeHeightSpotFraction = 0.225f;
        [SerializeField] private float extremeHeightMultiplier = 1.3f;
        [SerializeField] private float extremeHeightPatchSizeInHexes = 2.4f;
        [SerializeField] private Vector3 homeWorldCenter = Vector3.zero;

        public TerrainFrameSettings()
        {
        }

        public TerrainFrameSettings(
            float hexFlatToFlatMeters,
            int heightSeed,
            float heightAmplitudeMeters,
            float visualHeightMultiplier,
            float broadHeightScale,
            float noiseHeightScale,
            float extremeHeightSpotFraction = 0.225f,
            float extremeHeightMultiplier = 1.3f,
            float extremeHeightPatchSizeInHexes = 2.4f)
            : this(
                hexFlatToFlatMeters,
                heightSeed,
                heightAmplitudeMeters,
                visualHeightMultiplier,
                broadHeightScale,
                noiseHeightScale,
                Vector3.zero,
                extremeHeightSpotFraction,
                extremeHeightMultiplier,
                extremeHeightPatchSizeInHexes)
        {
        }

        public TerrainFrameSettings(
            float hexFlatToFlatMeters,
            int heightSeed,
            float heightAmplitudeMeters,
            float visualHeightMultiplier,
            float broadHeightScale,
            float noiseHeightScale,
            Vector3 homeWorldCenter,
            float extremeHeightSpotFraction = 0.225f,
            float extremeHeightMultiplier = 1.3f,
            float extremeHeightPatchSizeInHexes = 2.4f)
        {
            this.hexFlatToFlatMeters = hexFlatToFlatMeters;
            this.heightSeed = heightSeed;
            this.heightAmplitudeMeters = heightAmplitudeMeters;
            this.visualHeightMultiplier = visualHeightMultiplier;
            this.broadHeightScale = broadHeightScale;
            this.noiseHeightScale = noiseHeightScale;
            this.extremeHeightSpotFraction = extremeHeightSpotFraction;
            this.extremeHeightMultiplier = extremeHeightMultiplier;
            this.extremeHeightPatchSizeInHexes = extremeHeightPatchSizeInHexes;
            this.homeWorldCenter = homeWorldCenter;
        }

        public float HexFlatToFlatMeters => Mathf.Max(1f, hexFlatToFlatMeters);
        public float HexOuterRadiusMeters => HexFlatToFlatMeters / SqrtThree;
        public int HeightSeed => heightSeed;
        public float HeightAmplitudeMeters => Mathf.Max(0f, heightAmplitudeMeters);
        public float VisualHeightMultiplier => Mathf.Max(0f, visualHeightMultiplier);
        public float BroadHeightScale => broadHeightScale;
        public float NoiseHeightScale => noiseHeightScale;
        public float ExtremeHeightSpotFraction => Mathf.Clamp01(extremeHeightSpotFraction);
        public float ExtremeHeightMultiplier => Mathf.Max(1f, extremeHeightMultiplier);
        public float ExtremeHeightPatchSizeInHexes => Mathf.Max(0.5f, extremeHeightPatchSizeInHexes);
        public Vector3 HomeWorldCenter => homeWorldCenter;
    }
}
