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
        [SerializeField] private float heightAmplitudeMeters = 26f;
        [SerializeField] private float visualHeightMultiplier = 1.45f;
        [SerializeField] private float broadHeightScale = 0.012f;
        [SerializeField] private float noiseHeightScale = 0.024f;

        public TerrainFrameSettings()
        {
        }

        public TerrainFrameSettings(
            float hexFlatToFlatMeters,
            int heightSeed,
            float heightAmplitudeMeters,
            float visualHeightMultiplier,
            float broadHeightScale,
            float noiseHeightScale)
        {
            this.hexFlatToFlatMeters = hexFlatToFlatMeters;
            this.heightSeed = heightSeed;
            this.heightAmplitudeMeters = heightAmplitudeMeters;
            this.visualHeightMultiplier = visualHeightMultiplier;
            this.broadHeightScale = broadHeightScale;
            this.noiseHeightScale = noiseHeightScale;
        }

        public float HexFlatToFlatMeters => Mathf.Max(1f, hexFlatToFlatMeters);
        public float HexOuterRadiusMeters => HexFlatToFlatMeters / SqrtThree;
        public int HeightSeed => heightSeed;
        public float HeightAmplitudeMeters => Mathf.Max(0f, heightAmplitudeMeters);
        public float VisualHeightMultiplier => Mathf.Max(0f, visualHeightMultiplier);
        public float BroadHeightScale => broadHeightScale;
        public float NoiseHeightScale => noiseHeightScale;
    }
}
