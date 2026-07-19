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
    }
}
