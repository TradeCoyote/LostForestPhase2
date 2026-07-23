using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class RenderedSlotInstance
    {
        private readonly TerrainSurfaceSampler surfaceSampler;

        public RenderedSlotInstance(
            FieldSlotData fieldSlot,
            TerrainFrameData terrainFrameData,
            TerrainSlotData terrainSlot,
            GameObject root,
            HexTerrainMeshData terrainMeshData,
            TerrainSurfaceSampler surfaceSampler,
            int distanceFromCenter)
        {
            FieldSlot = fieldSlot;
            TerrainFrameData = terrainFrameData;
            TerrainSlot = terrainSlot;
            Root = root;
            TerrainMeshData = terrainMeshData;
            this.surfaceSampler = surfaceSampler;
            SetDistanceBand(distanceFromCenter);
        }

        public FieldSlotData FieldSlot { get; }
        public TerrainFrameData TerrainFrameData { get; }
        public TerrainSlotData TerrainSlot { get; }
        public GameObject Root { get; }
        public HexTerrainMeshData TerrainMeshData { get; }
        public int DistanceFromCenter { get; private set; }
        public string Address => FieldSlot == null ? string.Empty : FieldSlot.Address;
        public string TileIdLabel => FieldSlot == null ? string.Empty : FieldSlot.TileIdLabel;

        public bool TrySampleSurface(Vector3 worldXzPosition, out TerrainSurfaceSample sample)
        {
            if (surfaceSampler == null)
            {
                sample = default;
                return false;
            }

            return surfaceSampler.TrySample(worldXzPosition, out sample);
        }

        public void SetDistanceBand(int distanceFromCenter)
        {
            DistanceFromCenter = Mathf.Max(0, distanceFromCenter);
            // Future fog/LOD bands attach here without changing board-slot resolution.
        }

        public void Destroy()
        {
            if (Root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(Root);
            }
            else
            {
                Object.DestroyImmediate(Root);
            }
        }
    }
}
