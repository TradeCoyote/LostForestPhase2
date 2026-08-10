using UnityEngine;

namespace LostForest.Phase2.World
{
    public readonly struct TerrainSlotBuildRequest
    {
        public TerrainSlotBuildRequest(string label, Vector2Int axialCoordinate, Vector3 worldCenter)
        {
            Label = string.IsNullOrWhiteSpace(label) ? "Temporary" : label;
            AxialCoordinate = axialCoordinate;
            WorldCenter = worldCenter;
        }

        public string Label { get; }
        public Vector2Int AxialCoordinate { get; }
        public Vector3 WorldCenter { get; }
    }
}
